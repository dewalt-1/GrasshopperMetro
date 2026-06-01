using System;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// Sawtooth ramp LFO. Outputs (count / frequency) mod 1 in [0, 1) — wraps to 0 at integer phases.
    /// Driven by an optional upstream Count input (e.g. from Metro). If Count is unwired,
    /// self-schedules at ~30 Hz and uses an internal counter.
    /// </summary>
    public class PhasorComponent : ScheduledComponent
    {
        public PhasorComponent()
          : base(
                name: "Phasor",
                nickname: "Phasor",
                description: "Sawtooth ramp LFO driven by an optional Count input. Output in [0, 1) (wraps at 1).",
                category: "Params",
                subCategory: "Util")
        {
        }

        public override Guid ComponentGuid => new Guid("9E529CFD-7A93-465E-A4AF-46C9A0D4F3DC");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private int _internalCount = 0;
        private double _lastValue = 0.0;

        protected override void RegisterAdditionalInputs(GH_InputParamManager pManager)
        {
            int countIdx = pManager.AddIntegerParameter("Count", "C", "Optional upstream clock (e.g. from Metro/Tempo Count). If unwired, the component self-schedules at ~30 Hz.", GH_ParamAccess.item, 0);
            pManager[countIdx].Optional = true;
            pManager.AddIntegerParameter("Frequency", "F", "Counts per cycle. Higher = slower ramp. Clamped to >= 1.", GH_ParamAccess.item, 30);
        }

        protected override void RegisterAdditionalOutputs(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "Sawtooth ramp in [0, 1) — wraps to 0 at each integer phase.", GH_ParamAccess.item);
        }

        protected override int OnTick(IGH_DataAccess DA, bool isTick, bool isReset, bool isStart, bool isRunning)
        {
            bool hasExternalClock = Params.Input[2].SourceCount > 0;

            int count;
            if (hasExternalClock)
            {
                int externalCount = 0;
                DA.GetData(2, ref externalCount);
                _internalCount = externalCount;
                count = externalCount;
            }
            else
            {
                if (isReset || isStart) _internalCount = 0;
                if (isTick) _internalCount++;
                count = _internalCount;
            }

            int frequency = 30;
            DA.GetData(3, ref frequency);
            frequency = Math.Max(1, frequency);

            if (isRunning)
            {
                double phase = (double)count / frequency;
                _lastValue = phase - Math.Floor(phase);
            }

            DA.SetData(0, _lastValue);

            return hasExternalClock ? 0 : 33;
        }
    }
}
