using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace Metro
{
    public class MetroInfo : GH_AssemblyInfo
    {
        public override string Name => "Metro";
        public override Bitmap? Icon => null;
        public override string Description =>
            "A small suite of Max/MSP-style time-based components for Grasshopper. " +
            "Includes Metro (interval clock), Tempo (BPM clock), Delay (delayed bang), " +
            "Cycle (sine LFO), Phasor (sawtooth LFO), and Noise (random value generator).";
        public override Guid Id => new Guid("C5D3C09B-8FC1-45D4-8ADA-94835B13811F");
        public override string AuthorName => "Ben Drusinsky";
        public override string AuthorContact => "benodru@gmail.com";
    }
}
