using System;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// Base class for Grasshopper components that need a self-scheduling timer loop.
    /// Subclasses get Run + Reset inputs for free at indices 0 and 1, plus a SolveInstance
    /// that handles the entire scheduling-and-edge-detection state machine. They only need
    /// to register their additional inputs/outputs and implement OnTick.
    /// </summary>
    public abstract class ScheduledComponent : GH_Component
    {
        protected ScheduledComponent(string name, string nickname, string description,
                                     string category, string subCategory)
            : base(name, nickname, description, category, subCategory)
        {
        }

        // State preserved between solves. All reads/writes happen on the GH UI thread
        // (SolveInstance and scheduled-callback lambda both run on it), so no synchronization needed.
        private DateTime _startTime = DateTime.MinValue;
        private bool _wasRunning = false;
        private bool _tickPending = false;
        private bool _scheduled = false;
        private int _scheduleEpoch = 0;

        /// <summary>
        /// Wall-clock seconds since the current run began. Returns 0 when stopped.
        /// </summary>
        protected double Elapsed =>
            _startTime == DateTime.MinValue ? 0.0 : (DateTime.Now - _startTime).TotalSeconds;

        protected sealed override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Start/stop the timer.", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Reset", "X", "When true, restart the timer state on this solve.", GH_ParamAccess.item, false);
            RegisterAdditionalInputs(pManager);
        }

        protected sealed override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            RegisterAdditionalOutputs(pManager);
        }

        /// <summary>
        /// Register subclass-specific input parameters. They appear at index 2 and above.
        /// </summary>
        protected abstract void RegisterAdditionalInputs(GH_InputParamManager pManager);

        /// <summary>
        /// Register subclass-specific output parameters.
        /// </summary>
        protected abstract void RegisterAdditionalOutputs(GH_OutputParamManager pManager);

        /// <summary>
        /// Called on every solve. Read your inputs (starting at index 2) from DA, write your
        /// outputs to DA, then return the ms until the next scheduled callback (>= 1) or
        /// <= 0 to stop scheduling.
        /// </summary>
        /// <param name="DA">data access for reading inputs and writing outputs</param>
        /// <param name="isTick">true when this solve was triggered by the scheduled callback</param>
        /// <param name="isReset">true when Reset was true on this solve</param>
        /// <param name="isStart">true when Run just went false -> true on this solve</param>
        /// <param name="isRunning">true when Run is currently true (lets the subclass render correct stopped-state outputs without re-reading DA)</param>
        protected abstract int OnTick(IGH_DataAccess DA, bool isTick, bool isReset, bool isStart, bool isRunning);

        protected sealed override void SolveInstance(IGH_DataAccess DA)
        {
            // Capture and clear the "was this solve triggered by our timer?" flag at the top.
            bool isTick = _tickPending;
            _tickPending = false;

            bool run = false;
            bool reset = false;
            DA.GetData(0, ref run);
            DA.GetData(1, ref reset);

            bool isReset = reset;
            if (isReset)
            {
                _startTime = DateTime.Now;
                isTick = false; // a reset solve is not a tick
            }

            bool isStart = run && !_wasRunning;
            if (isStart)
            {
                _startTime = DateTime.Now;
                isTick = false; // first solve after starting is not a tick
            }

            // Run goes true -> false: clear start time AND invalidate any in-flight callback
            // so subsequent restarts don't see a stale tick.
            if (!run && _wasRunning)
            {
                _startTime = DateTime.MinValue;
                _scheduleEpoch++;
                _scheduled = false;
            }

            _wasRunning = run;

            // A stale callback firing after Run was toggled off is not a real tick.
            if (!run) isTick = false;

            int nextMs = OnTick(DA, isTick, isReset, isStart, run);

            // Schedule if running, the subclass asked for one, and either no callback is in
            // flight OR the subclass is signalling a re-arm via Reset/Start (which supersedes
            // any pending callback via epoch invalidation).
            if (run && (!_scheduled || isReset || isStart) && nextMs >= 1)
            {
                Schedule(nextMs);
            }
        }

        /// <summary>
        /// Schedule a callback in <paramref name="ms"/> milliseconds, cancelling any callback
        /// currently in flight (via epoch invalidation). Subclasses call this from inside
        /// <see cref="OnTick"/> when they need to (re-)schedule on an event the base can't
        /// detect itself — for example, edge-triggered components reacting to a Bang input.
        ///
        /// Has no effect if <paramref name="ms"/> is less than 1, the component isn't attached
        /// to a document, or Run is currently false.
        /// </summary>
        protected void ForceScheduleNext(int ms)
        {
            if (ms < 1) return;
            if (!_wasRunning) return; // _wasRunning was already updated to current run in SolveInstance.
            Schedule(ms);
        }

        private void Schedule(int ms)
        {
            _scheduleEpoch++;
            int myEpoch = _scheduleEpoch;
            _scheduled = true;
            OnPingDocument()?.ScheduleSolution(ms, _ =>
            {
                // Stale callback (superseded by Reset/Start re-arm, Run going false, or
                // another ForceScheduleNext call): bail.
                if (myEpoch != _scheduleEpoch) return;
                _scheduled = false;
                // Component may have been removed from the document between schedule and fire.
                if (OnPingDocument() == null) return;
                _tickPending = true;
                ExpireSolution(false);
            });
        }
    }
}
