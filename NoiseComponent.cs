using System;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// Uniform random value in [0, 1] regenerated each time the count changes.
    /// Driven by an optional upstream Count input (e.g. from Metro). If Count is unwired,
    /// self-schedules at ~30 Hz and uses an internal counter.
    /// </summary>
    public class NoiseComponent : ScheduledComponent
    {
        public NoiseComponent()
          : base(
                name: "Noise",
                nickname: "Noise",
                description: "Random value generator driven by an optional Count input. Output in [0, 1].",
                category: "Metro",
                subCategory: "Signals")
        {
        }

        public override Guid ComponentGuid => new Guid("624C9150-59A2-4E2D-A8DB-2F2F94DB9ECB");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private int _internalCount = 0;
        private int _lastCount = int.MinValue; // sentinel so the very first count change triggers a fresh random
        private double _lastValue = 0.0;
        private readonly Random _rng = new Random();

        protected override void RegisterAdditionalInputs(GH_InputParamManager pManager)
        {
            int countIdx = pManager.AddIntegerParameter("Count", "C", "Optional upstream clock (e.g. from Metro/Tempo Count). If unwired, the component self-schedules at ~30 Hz.", GH_ParamAccess.item, 0);
            pManager[countIdx].Optional = true;
        }

        protected override void RegisterAdditionalOutputs(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Value", "V", "Uniform random in [0, 1], regenerated on each count change.", GH_ParamAccess.item);
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

            if (isRunning && count != _lastCount)
            {
                _lastValue = _rng.NextDouble();
                _lastCount = count;
            }

            DA.SetData(0, _lastValue);

            return hasExternalClock ? 0 : 33;
        }
    }
}
