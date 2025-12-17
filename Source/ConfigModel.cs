using System;
using System.Collections.Generic;
using System.Text;

namespace CustomSols {
    public class ColorConfig {
        public string NormalHpColor { get; set; } = string.Empty;
        public string InternalHpColor { get; set; } = string.Empty;
        public string ExpRingOuterColor { get; set; } = string.Empty;
        public string ExpRingInnerColor { get; set; } = string.Empty;
        public string RageBarColor { get; set; } = string.Empty;
        public string RageBarFrameColor { get; set; } = string.Empty;
        public string ArrowLineBColor { get; set; } = string.Empty;
        public string ArrowGlowColor { get; set; } = string.Empty;
        public string ChiBallLeftLineColor { get; set; } = string.Empty;
        public string ButterflyRightLineColor { get; set; } = string.Empty;
        public string CoreCColor { get; set; } = string.Empty;
        public string CoreDColor { get; set; } = string.Empty;
    }

    public class BowConfig {
        public float[] NormalArrowLv1 { get; set; } = Array.Empty<float>();
        public float[] NormalArrowLv2 { get; set; } = Array.Empty<float>();
        public float[] NormalArrowLv3 { get; set; } = Array.Empty<float>();
    }
}
