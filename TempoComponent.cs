using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace Metro
{
    /// <summary>
    /// A Metro variant that specifies the tick rate in BPM (beats per minute) instead of
    /// milliseconds. ms-per-beat = 60000 / bpm.
    /// </summary>
    public class TempoComponent : ScheduledComponent
    {
        public TempoComponent()
          : base(
                name: "Tempo",
                nickname: "Tempo",
                description: "Like Metro, but specifies the tick rate in BPM (beats per minute).",
                category: "Params",
                subCategory: "Util")
        {
        }

        public override Guid ComponentGuid => new Guid("6A032D7B-60FD-4884-984B-DCA2534BB06E");
        public override GH_Exposure Exposure => GH_Exposure.primary;

        private int _count = 0;

        protected override void RegisterAdditionalInputs(GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("BPM", "BPM", "Beats per minute. Clamped to >= 1.", GH_ParamAccess.item, 120.0);
            int beIdx = pManager.AddIntegerParameter(
                "Bang Every", "BE",
                "List of beat positions to fire on, repeating in a cycle. Cycle length = max value in the list. " +
                "Examples: [3] -> beats 3, 6, 9, 12, ... (every 3rd beat). " +
                "[2, 4] -> beats 2, 4, 6, 8, ... (backbeat). " +
                "[1, 3] -> beats 1, 3, 4, 6, 7, 9, 10, 12, ... (positions 1 & 3 of a 3-beat cycle). " +
                "Empty / disconnected = every beat.",
                GH_ParamAccess.list);
            // Empty/disconnected list is valid (treated as "every beat"); don't warn the user.
            pManager[beIdx].Optional = true;
        }

        protected override void RegisterAdditionalOutputs(GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Count", "C", "Beat counter. 0 on start/reset, increments by 1 per beat.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Bang", "B", "True only on beats matching the rhythm pattern in Bang Every.", GH_ParamAccess.item);
        }

        protected override int OnTick(IGH_DataAccess DA, bool isTick, bool isReset, bool isStart, bool isRunning)
        {
            double bpm = 120.0;
            var bangEvery = new List<int>();
            DA.GetData(2, ref bpm);
            DA.GetDataList(3, bangEvery);
            bpm = Math.Max(1.0, bpm);

            if (isReset || isStart) _count = 0;
            if (isTick) _count++;

            DA.SetData(0, _count);
            DA.SetData(1, isTick && ShouldBang(_count, bangEvery));

            int msPerBeat = (int)Math.Round(60000.0 / bpm);
            return Math.Max(1, msPerBeat);
        }

        /// <summary>
        /// Treats <paramref name="beats"/> as a repeating rhythm pattern: list of 1-indexed
        /// positions to fire on within a cycle whose length = max valid value in the list.
        /// Example: beats=[2, 4] -> cycle=4, fires on counts 2, 4, 6, 8, ...
        /// Empty or all-invalid = every beat.
        /// </summary>
        private static bool ShouldBang(int count, IReadOnlyList<int> beats)
        {
            int cycle = 0;
            for (int i = 0; i < beats.Count; i++)
            {
                int b = beats[i];
                if (b < 1) continue;
                if (b > cycle) cycle = b;
            }
            if (cycle == 0) return true;

            int pos = ((count - 1) % cycle) + 1;
            for (int i = 0; i < beats.Count; i++)
            {
                if (beats[i] == pos) return true;
            }
            return false;
        }
    }
}
