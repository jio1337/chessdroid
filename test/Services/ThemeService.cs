namespace ChessDroid.Services
{
    /// <summary>
    /// Color scheme for application theme
    /// </summary>
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

        // Move classification symbol colors (same for both themes)
        public static readonly Color BrilliantColor = Color.FromArgb(26, 179, 148);   // Cyan/teal
        public static readonly Color BlunderColor = Color.FromArgb(202, 52, 49);      // Red
        public static readonly Color InaccuracyColor = Color.FromArgb(247, 199, 72);  // Yellow
        public static readonly Color MistakeColor = Color.FromArgb(232, 106, 51);     // Orange
    }

    /// <summary>
    /// Manages application theme (Dark/Light mode)
    /// Extracted from MainForm to centralize theme management
    /// </summary>
    public class ThemeService
    {
        private static readonly ColorScheme DarkScheme = new ColorScheme
        {
            // Main form
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

            // Analysis Board
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
            // Main form
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

            // Analysis Board
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

        /// <summary>
        /// Applies theme to MainForm controls
        /// </summary>
        public static void ApplyTheme(Form form, Label labelStatus, Button button1, Button buttonReset,
            Button buttonSettings, RichTextBox richTextBoxConsole, CheckBox chkWhiteTurn, CheckBox chkPin, bool isDarkMode)
        {
            // Suspend layout to prevent flickering
            form.SuspendLayout();

            ColorScheme scheme = isDarkMode ? DarkScheme : LightScheme;

            // Apply colors
            form.BackColor = scheme.BackgroundColor;
            labelStatus.BackColor = scheme.LabelBackColor;
            labelStatus.ForeColor = scheme.LabelForeColor;
            button1.BackColor = scheme.Button1BackColor;
            button1.ForeColor = scheme.Button1ForeColor;
            buttonReset.BackColor = scheme.ButtonResetBackColor;
            buttonReset.ForeColor = scheme.ButtonResetForeColor;
            buttonSettings.BackColor = scheme.ButtonSettingsBackColor;
            buttonSettings.ForeColor = scheme.ButtonSettingsForeColor;
            richTextBoxConsole.BackColor = scheme.ConsoleBackColor;
            richTextBoxConsole.ForeColor = scheme.ConsoleForeColor;
            chkWhiteTurn.BackColor = scheme.CheckboxBackColor;
            chkWhiteTurn.ForeColor = scheme.CheckboxForeColor;
            chkPin.BackColor = scheme.CheckboxBackColor;
            chkPin.ForeColor = scheme.CheckboxForeColor;

            // Resume layout
            form.ResumeLayout();
        }

        /// <summary>
        /// Gets the color scheme for a theme
        /// </summary>
        public static ColorScheme GetColorScheme(bool isDarkMode)
        {
            return isDarkMode ? DarkScheme : LightScheme;
        }
    }
}