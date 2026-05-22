namespace ChessDroid.Services
{
    public class ColorScheme
    {
        // Main form colors
        public Color BackgroundColor { get; set; }
        public Color LabelBackColor { get; set; }
        public Color LabelForeColor { get; set; }
        public Color Button1BackColor { get; set; }
        public Color Button1ForeColor { get; set; }
        public Color ButtonResetBackColor { get; set; }
        public Color ButtonResetForeColor { get; set; }
        public Color ButtonSettingsBackColor { get; set; }
        public Color ButtonSettingsForeColor { get; set; }
        public Color ConsoleBackColor { get; set; }
        public Color ConsoleForeColor { get; set; }
        public Color CheckboxBackColor { get; set; }
        public Color CheckboxForeColor { get; set; }

        // Analysis Board colors
        public Color FormBackColor { get; set; }
        public Color PanelColor { get; set; }
        public Color TextColor { get; set; }
        public Color StatusColor { get; set; }
        public Color ButtonBackColor { get; set; }
        public Color ButtonForeColor { get; set; }
        public Color AnalyzeButtonBackColor { get; set; }
        public Color GroupBoxBackColor { get; set; }
        public Color ClockBackColor { get; set; }
        public Color ClockActiveBackColor { get; set; }
        public Color StartMatchButtonBackColor { get; set; }
        public Color StopMatchButtonBackColor { get; set; }
        public Color WhiteClockForeColor { get; set; }
        public Color BlackClockForeColor { get; set; }

        // Move classification symbol colors (same for all themes)
        public static readonly Color BrilliantColor = Color.FromArgb(26, 179, 148);
        public static readonly Color BlunderColor = Color.FromArgb(202, 52, 49);
        public static readonly Color InaccuracyColor = Color.FromArgb(247, 199, 72);
        public static readonly Color MistakeColor = Color.FromArgb(232, 106, 51);
        public static readonly Color OnlyMoveColor = Color.FromArgb(91, 139, 245);
        public static readonly Color BestMoveColor = Color.FromArgb(120, 210, 80);
        public static readonly Color ExcellentMoveColor = Color.FromArgb(160, 190, 100);
        public static readonly Color GoodMoveColor = Color.FromArgb(140, 160, 120);
    }

    public class ThemeService
    {
        public static readonly string[] ThemeNames = { "Dark", "Light", "Cyberpunk", "Dracula", "Nord", "Sepia" };

        private static readonly ColorScheme DarkScheme = new ColorScheme
        {
            BackgroundColor = Color.FromArgb(30, 30, 35),
            LabelBackColor = Color.FromArgb(60, 60, 65),
            LabelForeColor = Color.White,
            Button1BackColor = Color.FromArgb(45, 45, 48),
            Button1ForeColor = Color.Thistle,
            ButtonResetBackColor = Color.FromArgb(45, 45, 48),
            ButtonResetForeColor = Color.LightCoral,
            ButtonSettingsBackColor = Color.FromArgb(45, 45, 48),
            ButtonSettingsForeColor = Color.Orange,
            ConsoleBackColor = Color.FromArgb(30, 30, 35),
            ConsoleForeColor = Color.LightGray,
            CheckboxBackColor = Color.FromArgb(30, 30, 35),
            CheckboxForeColor = Color.White,

            FormBackColor = Color.FromArgb(30, 30, 35),
            PanelColor = Color.FromArgb(40, 40, 48),
            TextColor = Color.FromArgb(220, 220, 220),
            StatusColor = Color.Gray,
            ButtonBackColor = Color.FromArgb(50, 50, 58),
            ButtonForeColor = Color.FromArgb(200, 200, 200),
            AnalyzeButtonBackColor = Color.FromArgb(60, 90, 140),
            GroupBoxBackColor = Color.FromArgb(35, 35, 42),
            ClockBackColor = Color.FromArgb(50, 50, 58),
            ClockActiveBackColor = Color.FromArgb(40, 80, 50),
            StartMatchButtonBackColor = Color.FromArgb(40, 100, 60),
            StopMatchButtonBackColor = Color.FromArgb(140, 50, 50),
            WhiteClockForeColor = Color.White,
            BlackClockForeColor = Color.LightGray
        };

        private static readonly ColorScheme LightScheme = new ColorScheme
        {
            BackgroundColor = Color.WhiteSmoke,
            LabelBackColor = Color.Gainsboro,
            LabelForeColor = Color.Black,
            Button1BackColor = Color.Lavender,
            Button1ForeColor = Color.DarkSlateBlue,
            ButtonResetBackColor = Color.MistyRose,
            ButtonResetForeColor = Color.DarkRed,
            ButtonSettingsBackColor = Color.LightYellow,
            ButtonSettingsForeColor = Color.DarkGoldenrod,
            ConsoleBackColor = Color.AliceBlue,
            ConsoleForeColor = Color.Black,
            CheckboxBackColor = Color.WhiteSmoke,
            CheckboxForeColor = Color.Black,

            FormBackColor = Color.FromArgb(245, 245, 245),
            PanelColor = Color.White,
            TextColor = Color.Black,
            StatusColor = Color.DimGray,
            ButtonBackColor = Color.FromArgb(230, 230, 230),
            ButtonForeColor = Color.Black,
            AnalyzeButtonBackColor = Color.FromArgb(70, 130, 180),
            GroupBoxBackColor = Color.White,
            ClockBackColor = Color.FromArgb(240, 240, 240),
            ClockActiveBackColor = Color.FromArgb(200, 240, 200),
            StartMatchButtonBackColor = Color.FromArgb(60, 140, 80),
            StopMatchButtonBackColor = Color.FromArgb(180, 60, 60),
            WhiteClockForeColor = Color.Black,
            BlackClockForeColor = Color.DimGray
        };

        // Neon-on-black — 1999 or 2077, take your pick
        private static readonly ColorScheme CyberpunkScheme = new ColorScheme
        {
            BackgroundColor = Color.FromArgb(10, 10, 20),
            LabelBackColor = Color.FromArgb(20, 20, 40),
            LabelForeColor = Color.FromArgb(0, 240, 255),
            Button1BackColor = Color.FromArgb(20, 20, 40),
            Button1ForeColor = Color.FromArgb(255, 0, 180),
            ButtonResetBackColor = Color.FromArgb(20, 20, 40),
            ButtonResetForeColor = Color.FromArgb(255, 60, 60),
            ButtonSettingsBackColor = Color.FromArgb(20, 20, 40),
            ButtonSettingsForeColor = Color.FromArgb(255, 200, 0),
            ConsoleBackColor = Color.FromArgb(5, 5, 15),
            ConsoleForeColor = Color.FromArgb(0, 255, 160),
            CheckboxBackColor = Color.FromArgb(10, 10, 20),
            CheckboxForeColor = Color.FromArgb(0, 240, 255),

            FormBackColor = Color.FromArgb(10, 10, 20),
            PanelColor = Color.FromArgb(18, 18, 35),
            TextColor = Color.FromArgb(0, 240, 255),
            StatusColor = Color.FromArgb(100, 100, 160),
            ButtonBackColor = Color.FromArgb(25, 25, 50),
            ButtonForeColor = Color.FromArgb(200, 200, 255),
            AnalyzeButtonBackColor = Color.FromArgb(120, 0, 180),
            GroupBoxBackColor = Color.FromArgb(15, 15, 28),
            ClockBackColor = Color.FromArgb(20, 20, 40),
            ClockActiveBackColor = Color.FromArgb(0, 80, 120),
            StartMatchButtonBackColor = Color.FromArgb(0, 120, 80),
            StopMatchButtonBackColor = Color.FromArgb(160, 0, 80),
            WhiteClockForeColor = Color.FromArgb(0, 240, 255),
            BlackClockForeColor = Color.FromArgb(180, 0, 255)
        };

        // Dracula — the classic dark theme for people with good taste
        private static readonly ColorScheme DraculaScheme = new ColorScheme
        {
            BackgroundColor = Color.FromArgb(40, 42, 54),
            LabelBackColor = Color.FromArgb(68, 71, 90),
            LabelForeColor = Color.FromArgb(248, 248, 242),
            Button1BackColor = Color.FromArgb(68, 71, 90),
            Button1ForeColor = Color.FromArgb(189, 147, 249),
            ButtonResetBackColor = Color.FromArgb(68, 71, 90),
            ButtonResetForeColor = Color.FromArgb(255, 85, 85),
            ButtonSettingsBackColor = Color.FromArgb(68, 71, 90),
            ButtonSettingsForeColor = Color.FromArgb(255, 184, 108),
            ConsoleBackColor = Color.FromArgb(40, 42, 54),
            ConsoleForeColor = Color.FromArgb(248, 248, 242),
            CheckboxBackColor = Color.FromArgb(40, 42, 54),
            CheckboxForeColor = Color.FromArgb(248, 248, 242),

            FormBackColor = Color.FromArgb(40, 42, 54),
            PanelColor = Color.FromArgb(55, 57, 72),
            TextColor = Color.FromArgb(248, 248, 242),
            StatusColor = Color.FromArgb(98, 114, 164),
            ButtonBackColor = Color.FromArgb(68, 71, 90),
            ButtonForeColor = Color.FromArgb(248, 248, 242),
            AnalyzeButtonBackColor = Color.FromArgb(98, 114, 164),
            GroupBoxBackColor = Color.FromArgb(48, 50, 64),
            ClockBackColor = Color.FromArgb(68, 71, 90),
            ClockActiveBackColor = Color.FromArgb(40, 90, 60),
            StartMatchButtonBackColor = Color.FromArgb(60, 130, 75),
            StopMatchButtonBackColor = Color.FromArgb(150, 50, 50),
            WhiteClockForeColor = Color.FromArgb(248, 248, 242),
            BlackClockForeColor = Color.FromArgb(189, 147, 249)
        };

        // Nord — deep navy ice, frost text, Arctic cold shoulder energy
        private static readonly ColorScheme NordScheme = new ColorScheme
        {
            BackgroundColor = Color.FromArgb(22, 32, 50),
            LabelBackColor = Color.FromArgb(35, 50, 72),
            LabelForeColor = Color.FromArgb(136, 192, 208),
            Button1BackColor = Color.FromArgb(35, 50, 72),
            Button1ForeColor = Color.FromArgb(143, 188, 187),
            ButtonResetBackColor = Color.FromArgb(35, 50, 72),
            ButtonResetForeColor = Color.FromArgb(191, 97, 106),
            ButtonSettingsBackColor = Color.FromArgb(35, 50, 72),
            ButtonSettingsForeColor = Color.FromArgb(235, 203, 139),
            ConsoleBackColor = Color.FromArgb(18, 26, 42),
            ConsoleForeColor = Color.FromArgb(136, 192, 208),
            CheckboxBackColor = Color.FromArgb(22, 32, 50),
            CheckboxForeColor = Color.FromArgb(136, 192, 208),

            FormBackColor = Color.FromArgb(22, 32, 50),
            PanelColor = Color.FromArgb(32, 46, 68),
            TextColor = Color.FromArgb(136, 192, 208),
            StatusColor = Color.FromArgb(88, 130, 160),
            ButtonBackColor = Color.FromArgb(42, 60, 88),
            ButtonForeColor = Color.FromArgb(200, 220, 235),
            AnalyzeButtonBackColor = Color.FromArgb(60, 100, 150),
            GroupBoxBackColor = Color.FromArgb(28, 40, 60),
            ClockBackColor = Color.FromArgb(42, 60, 88),
            ClockActiveBackColor = Color.FromArgb(35, 85, 75),
            StartMatchButtonBackColor = Color.FromArgb(40, 100, 80),
            StopMatchButtonBackColor = Color.FromArgb(140, 50, 55),
            WhiteClockForeColor = Color.FromArgb(216, 235, 245),
            BlackClockForeColor = Color.FromArgb(143, 188, 187)
        };

        // Sepia — aged parchment and dark ink, not a beige accident
        private static readonly ColorScheme SepiaScheme = new ColorScheme
        {
            BackgroundColor = Color.FromArgb(220, 198, 152),
            LabelBackColor = Color.FromArgb(195, 168, 115),
            LabelForeColor = Color.FromArgb(45, 25, 8),
            Button1BackColor = Color.FromArgb(195, 168, 115),
            Button1ForeColor = Color.FromArgb(90, 50, 15),
            ButtonResetBackColor = Color.FromArgb(195, 168, 115),
            ButtonResetForeColor = Color.FromArgb(160, 50, 30),
            ButtonSettingsBackColor = Color.FromArgb(195, 168, 115),
            ButtonSettingsForeColor = Color.FromArgb(120, 80, 10),
            ConsoleBackColor = Color.FromArgb(240, 220, 170),
            ConsoleForeColor = Color.FromArgb(45, 25, 8),
            CheckboxBackColor = Color.FromArgb(220, 198, 152),
            CheckboxForeColor = Color.FromArgb(45, 25, 8),

            FormBackColor = Color.FromArgb(220, 198, 152),
            PanelColor = Color.FromArgb(242, 225, 182),
            TextColor = Color.FromArgb(45, 25, 8),
            StatusColor = Color.FromArgb(110, 80, 40),
            ButtonBackColor = Color.FromArgb(195, 168, 115),
            ButtonForeColor = Color.FromArgb(45, 25, 8),
            AnalyzeButtonBackColor = Color.FromArgb(148, 98, 20),
            GroupBoxBackColor = Color.FromArgb(232, 212, 165),
            ClockBackColor = Color.FromArgb(195, 168, 115),
            ClockActiveBackColor = Color.FromArgb(165, 135, 75),
            StartMatchButtonBackColor = Color.FromArgb(85, 115, 55),
            StopMatchButtonBackColor = Color.FromArgb(160, 60, 40),
            WhiteClockForeColor = Color.FromArgb(45, 25, 8),
            BlackClockForeColor = Color.FromArgb(90, 50, 15)
        };

        public static ColorScheme GetColorScheme(string theme) => theme switch
        {
            "Cyberpunk" => CyberpunkScheme,
            "Dracula"   => DraculaScheme,
            "Nord"      => NordScheme,
            "Sepia"     => SepiaScheme,
            "Light"     => LightScheme,
            _           => DarkScheme
        };

        public static bool IsDarkTheme(string? theme) => theme switch
        {
            "Light" or "Sepia" => false,
            _                  => true
        };
    }
}
