namespace ChessDroid.Services
{
    /// <summary>
    /// Color scheme for application theme
    /// </summary>
    public class ColorScheme
    {
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
    }

    /// <summary>
    /// Manages application theme (Dark/Light mode)
    /// Extracted from MainForm to centralize theme management
    /// </summary>
    public class ThemeService
    {
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
            CheckboxForeColor = Color.White
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
            CheckboxForeColor = Color.Black
        };

        /// <summary>
        /// Applies theme to MainForm controls
        /// </summary>
        public static void ApplyTheme(Form form, Label labelStatus, Button button1, Button buttonReset,
            Button buttonSettings, RichTextBox richTextBoxConsole, CheckBox chkWhiteTurn, bool isDarkMode)
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