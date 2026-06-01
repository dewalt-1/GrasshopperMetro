using System;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// Sine wave LFO. Outputs sin(2π · count / frequency) in [-1, 1].
    /// Driven by an optional upstream Count input (e.g. from Metro). If Count is unwired,
    /// self-schedules at ~30 Hz and uses an internal counter.
    /// </summary>
    public class CycleComponent : ScheduledComponent
    {
        public CycleComponent()
          : base(
                name: "Cycle",
                nickname: "Cycle",
                description: "Sine wave LFO driven by an optional Count input. Output in [-1, 1].",
                category: "Params",
                subCategory: "Util")
        {
        }

        public override Guid ComponentGuid => new Guid("8DEDB17B-C277-4D73-8810-BC2CEE54E438");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private int _internalCount = 0;
        private double _lastValue = 0.0;

        protected override void RegisterAdditionalInputs(GH_InputParamManager pManager)
        {
            int countIdx = pManager.AddIntegerParameter("Count", "C", "Optional upstream clock (e.g. from Metro/Tempo Count). If unwired, the component self-schedules at ~30 Hz.", GH_ParamAccess.item, 0);
            pManager[countIdx].Optional = true;
            pManager.AddIntegerParameter("Frequency", "F", "Counts per cycle. Higher = slower wave. Clamped to >= 1.", GH_ParamAccess.item, 30);
        }

        protected override void RegisterAdditionalOutputs(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "Sine wave value in [-1, 1].", GH_ParamAccess.item);
        }

        protected override int OnTick(IGH_DataAccess DA, bool isTick, bool isReset, bool isStart, bool isRunning)
        {
            bool hasExternalClock = Params.Input[2].SourceCount > 0;

            int count;
            if (hasExternalClock)
            {
                int externalCount = 0;
                DA.GetData(2, ref externalCount);
                _internalCount = externalCount; // sync for seamless mode switching
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
                _lastValue = Math.Sin(2.0 * Math.PI * count / frequency);
            }

            DA.SetData(0, _lastValue);

            return hasExternalClock ? 0 : 33; // 0 = passive, 33 = self-schedule at ~30 Hz
        }
    }
}
