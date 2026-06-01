using System;
using Grasshopper.Kernel;

namespace Metro
{
    public class MetroComponent : GH_Component
    {
        public MetroComponent()
          : base(
                name: "Metro",
                nickname: "Metro",
                description: "A clock that fires at a fixed interval. Ports Max/MSP's metro object.",
                category: "Params",
                subCategory: "Util")
        {
        }

        public override Guid ComponentGuid => new Guid("D4351991-185F-471D-9E7B-3B94DD301E48");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        // State preserved between solves. All reads/writes happen on the GH UI thread
        // (SolveInstance and ScheduleCallback both run on it), so no synchronization needed.
        private int _count = 0;
        private DateTime _startTime = DateTime.MinValue;
        private bool _wasRunning = false;
        private bool _bangNextSolve = false;
        private bool _scheduled = false;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Start/stop the timer.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Interval", "I", "Time between ticks in milliseconds (min 1).", GH_ParamAccess.item, 500);
            pManager.AddBooleanParameter("Reset", "X", "When true, zero the counter and elapsed time.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Max Count", "M", "Auto-stop after N ticks. 0 means run forever.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("Bang Every", "BE", "Fire Bang only every Nth tick. 1 = every tick (default).", GH_ParamAccess.item, 1);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Count", "C", "Tick counter. 0 on start/reset, increments by 1 each tick.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Elapsed", "E", "Wall-clock seconds since the current run began.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bang", "B", "True when this solve was triggered by a timer tick.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // Capture and clear the "was this solve triggered by our timer?" flag at the top.
            bool isTick = _bangNextSolve;
            _bangNextSolve = false;

            bool run = false;
            int interval = 500;
            bool reset = false;
            int maxCount = 0;
            int bangEvery = 1;
            DA.GetData(0, ref run);
            DA.GetData(1, ref interval);
            DA.GetData(2, ref reset);
            DA.GetData(3, ref maxCount);
            DA.GetData(4, ref bangEvery);

            interval = Math.Max(1, interval);
            bangEvery = Math.Max(1, bangEvery);

            if (reset)
            {
                _count = 0;
                _startTime = DateTime.Now;
                isTick = false; // a reset solve is not a tick
            }

            if (run && !_wasRunning)
            {
                // Run goes false -> true.
                _count = 0;
                _startTime = DateTime.Now;
                isTick = false; // first solve after starting is not a tick
            }

            _wasRunning = run;

            // Apply a tick if one is pending and we're still running.
            bool tickFired = false;
            if (isTick && run)
            {
                if (maxCount <= 0 || _count < maxCount)
                {
                    _count++;
                    tickFired = true;
                }
            }

            // Schedule the next tick if running, not already queued, and not at the cap.
            // This prevents runaway scheduling when user changes inputs mid-run.
            bool atMax = maxCount > 0 && _count >= maxCount;
            if (run && !_scheduled && !atMax)
            {
                _scheduled = true;
                OnPingDocument()?.ScheduleSolution(interval, ScheduleCallback);
            }

            double elapsed = _startTime == DateTime.MinValue
                ? 0.0
                : (DateTime.Now - _startTime).TotalSeconds;

            DA.SetData(0, _count);
            DA.SetData(1, elapsed);
            DA.SetData(2, tickFired && _count % bangEvery == 0);
        }

        private void ScheduleCallback(GH_Document doc)
        {
            _scheduled = false;
            // Component may have been removed from the document between schedule and fire —
            // bail out without expiring an orphan.
            if (OnPingDocument() == null) return;
            _bangNextSolve = true;
            ExpireSolution(false);
        }
    }
}
