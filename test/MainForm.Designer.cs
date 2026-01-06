namespace ChessDroid
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            button1 = new Button();
            buttonReset = new Button();
            buttonSettings = new Button();
            richTextBoxConsole = new RichTextBox();
            labelStatus = new Label();
            chkWhiteTurn = new CheckBox();
            SuspendLayout();
            // 
            // button1
            // 
            button1.BackColor = Color.Lavender;
            button1.Cursor = Cursors.Hand;
            button1.FlatStyle = FlatStyle.Popup;
            button1.Font = new Font("Courier New", 9F, FontStyle.Bold);
            button1.ForeColor = Color.DarkSlateBlue;
            button1.Location = new Point(12, 12);
            button1.Name = "button1";
            button1.Size = new Size(175, 35);
            button1.TabIndex = 0;
            button1.Text = "Show Lines";
            button1.UseVisualStyleBackColor = false;
            button1.Click += button1_Click_1;
            // 
            // buttonReset
            // 
            buttonReset.BackColor = Color.MistyRose;
            buttonReset.Cursor = Cursors.Hand;
            buttonReset.FlatStyle = FlatStyle.Popup;
            buttonReset.Font = new Font("Courier New", 9F, FontStyle.Bold);
            buttonReset.ForeColor = Color.DarkRed;
            buttonReset.Location = new Point(197, 12);
            buttonReset.Name = "buttonReset";
            buttonReset.Size = new Size(175, 35);
            buttonReset.TabIndex = 1;
            buttonReset.Text = "Reset App";
            buttonReset.UseVisualStyleBackColor = false;
            buttonReset.Click += buttonReset_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.BackColor = Color.LightYellow;
            buttonSettings.Cursor = Cursors.Hand;
            buttonSettings.FlatStyle = FlatStyle.Popup;
            buttonSettings.Font = new Font("Courier New", 9F, FontStyle.Bold);
            buttonSettings.ForeColor = Color.DarkGoldenrod;
            buttonSettings.Location = new Point(12, 293);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(360, 35);
            buttonSettings.TabIndex = 3;
            buttonSettings.Text = "⚙ Settings";
            buttonSettings.UseVisualStyleBackColor = false;
            buttonSettings.Click += buttonSettings_Click;
            // 
            // richTextBoxConsole
            // 
            richTextBoxConsole.BackColor = Color.AliceBlue;
            richTextBoxConsole.Font = new Font("Courier New", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            richTextBoxConsole.ForeColor = Color.Black;
            richTextBoxConsole.Location = new Point(12, 58);
            richTextBoxConsole.Name = "richTextBoxConsole";
            richTextBoxConsole.ReadOnly = true;
            richTextBoxConsole.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxConsole.Size = new Size(360, 225);
            richTextBoxConsole.TabIndex = 2;
            richTextBoxConsole.Text = "chessdroid://waiting";
            // 
            // labelStatus
            // 
            labelStatus.AutoSize = true;
            labelStatus.BackColor = Color.Transparent;
            labelStatus.Font = new Font("Courier New", 7.5F);
            labelStatus.ForeColor = Color.Black;
            labelStatus.Location = new Point(12, 338);
            labelStatus.Name = "labelStatus";
            labelStatus.Size = new Size(35, 12);
            labelStatus.TabIndex = 4;
            labelStatus.Text = "Ready";
            // 
            // chkWhiteTurn
            // 
            chkWhiteTurn.AutoSize = true;
            chkWhiteTurn.BackColor = Color.WhiteSmoke;
            chkWhiteTurn.Checked = true;
            chkWhiteTurn.CheckState = CheckState.Checked;
            chkWhiteTurn.Font = new Font("Courier New", 8.25F, FontStyle.Bold);
            chkWhiteTurn.ForeColor = Color.Black;
            chkWhiteTurn.Location = new Point(255, 335);
            chkWhiteTurn.Name = "chkWhiteTurn";
            chkWhiteTurn.Size = new Size(117, 18);
            chkWhiteTurn.TabIndex = 5;
            chkWhiteTurn.Text = "White to move";
            chkWhiteTurn.UseVisualStyleBackColor = false;
            chkWhiteTurn.CheckedChanged += chkWhiteTurn_CheckedChanged;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            BackColor = Color.WhiteSmoke;
            ClientSize = new Size(385, 360);
            Controls.Add(chkWhiteTurn);
            Controls.Add(labelStatus);
            Controls.Add(buttonSettings);
            Controls.Add(richTextBoxConsole);
            Controls.Add(buttonReset);
            Controls.Add(button1);
            Font = new Font("Courier New", 8.25F, FontStyle.Bold);
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "chessdroid v2.0.0";
            TopMost = true;
            FormClosed += Form1_FormClosed;
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Button buttonReset;
        private Button button1;
        private Button buttonSettings;
        private RichTextBox richTextBoxConsole;
        private Label labelStatus;
        private CheckBox chkWhiteTurn;
    }
}
