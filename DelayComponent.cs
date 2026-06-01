using System;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// Delays an incoming Bang by N milliseconds. On every rising edge of the Bang input
    /// (false -> true) while Run is true, schedules a delayed fire. When the wait elapses,
    /// outputs a single Bang and increments Count. Re-triggering before the previous fire
    /// cancels the old wait and restarts from now.
    ///
    /// Note: the upstream Bang source must actually pulse (true -> false -> true). To chain
    /// from Metro or Tempo, set their Bang Every input to >= 2 so the source pulses cleanly.
    /// </summary>
    public class DelayComponent : ScheduledComponent
    {
        public DelayComponent()
          : base(
                name: "Delay",
                nickname: "Delay",
                description: "Delays an incoming Bang by N milliseconds. Edge-triggered on the Bang input.",
                category: "Params",
                subCategory: "Util")
        {
        }

        public override Guid ComponentGuid => new Guid("E75C87B3-1F80-43C4-BE04-0B9EBD15E12C");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private int _count = 0;
        private bool _lastBang = false;

        protected override void RegisterAdditionalInputs(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Bang", "B", "Trigger input. Each rising edge (false -> true) while Run is true schedules a delayed fire.", GH_ParamAccess.item, false);
            pManager.AddIntegerParameter("Delay", "D", "Wait time in milliseconds before re-emitting Bang. Clamped to >= 1.", GH_ParamAccess.item, 1000);
        }

        protected override void RegisterAdditionalOutputs(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Count", "C", "Number of times Delay has fired since start/reset.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bang", "B", "True on the single solve where the delayed bang fires.", GH_ParamAccess.item);
        }

        protected override int OnTick(IGH_DataAccess DA, bool isTick, bool isReset, bool isStart, bool isRunning)
        {
            bool bangIn = false;
            int delay = 1000;
            DA.GetData(2, ref bangIn);
            DA.GetData(3, ref delay);
            delay = Math.Max(1, delay);

            // Reset/start zero the count and the edge-detection state so the next bang is fresh.
            if (isReset || isStart)
            {
                _count = 0;
                _lastBang = false;
            }

            // Detect rising edge on the Bang input.
            bool bangEdge = isRunning && bangIn && !_lastBang;
            _lastBang = bangIn;

            // The base's scheduled callback fired — emit a Bang, increment Count.
            if (isTick) _count++;

            DA.SetData(0, _count);
            DA.SetData(1, isTick);

            // On a fresh rising edge, force a (re-)schedule. This cancels any pending callback
            // via epoch invalidation, so back-to-back triggers always reflect the latest input.
            if (bangEdge)
            {
                ForceScheduleNext(delay);
            }

            // Always return 0 — Delay never wants the "regular" periodic scheduling path.
            return 0;
        }
    }
}
