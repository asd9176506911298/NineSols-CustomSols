namespace CustomSols {
    public class MainConfig {
        // 對應原本的 color.json 內容
        public ColorConfig Colors { get; set; } = new ColorConfig();
        // 對應原本的 parry.json 內容
        public ParryConfig Parry { get; set; } = new ParryConfig();
        // 對應原本的 bow.json 內容
        public BowConfig Bow { get; set; } = new BowConfig();
    }

    public class ColorConfig {
        public string NormalHpColor { get; set; } = "#FF0000"; // 可設定預設值
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

    public class ParryConfig {
        public string UCCharging1Color { get; set; } = string.Empty;
        public string UCCharging2Color { get; set; } = string.Empty;
        public string UCSuccess1Color { get; set; } = string.Empty;
        public string UCSuccess2Color { get; set; } = string.Empty;
        public string AirParryColor { get; set; } = string.Empty;
        public string UCParryColor { get; set; } = string.Empty;
        public string DashColor { get; set; } = string.Empty;
    }

    public class BowConfig {
        public float[] NormalArrowLv1 { get; set; } = new float[] { 0, 0, 0 };
        public float[] NormalArrowLv2 { get; set; } = new float[] { 0, 0, 0 };
        public float[] NormalArrowLv3 { get; set; } = new float[] { 0, 0, 0 };
    }
}