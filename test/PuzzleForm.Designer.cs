using ChessDroid.Controls;

namespace ChessDroid
{
    partial class PuzzleForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            leftPanel = new Panel();
            boardControl = new ChessBoardControl();
            lblTurn = new Label();
            btnHint = new Button();
            btnSkip = new Button();
            btnNext = new Button();
            btnFlipBoard = new Button();
            btnRetry = new Button();
            btnAnalyze = new Button();
            lblStatus = new Label();
            rightPanel = new Panel();
            grpPuzzleInfo = new GroupBox();
            lblPuzzleRating = new Label();
            lblPuzzleThemes = new Label();
            lblPuzzleProgress = new Label();
            grpFeedback = new GroupBox();
            lblFeedback = new Label();
            grpFilters = new GroupBox();
            lblMinRating = new Label();
            numMinRating = new NumericUpDown();
            lblMaxRating = new Label();
            numMaxRating = new NumericUpDown();
            lblThemeFilter = new Label();
            cmbThemeFilter = new ComboBox();
            grpStats = new GroupBox();
            btnResetStats = new Button();
            lblStatsRating = new Label();
            lblStatsSolved = new Label();
            lblStatsStreak = new Label();
            lblStatsHints = new Label();
            lblStatsAccuracy = new Label();

            mainLayout.SuspendLayout();
            leftPanel.SuspendLayout();
            rightPanel.SuspendLayout();
            grpPuzzleInfo.SuspendLayout();
            grpFeedback.SuspendLayout();
            grpFilters.SuspendLayout();
            grpStats.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numMinRating).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRating).BeginInit();
            SuspendLayout();

            //
            // mainLayout
            //
            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(rightPanel, 1, 0);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(5);
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(900, 650);
            mainLayout.TabIndex = 0;

            //
            // leftPanel
            //
            leftPanel.Controls.Add(boardControl);
            leftPanel.Controls.Add(lblTurn);
            leftPanel.Controls.Add(btnHint);
            leftPanel.Controls.Add(btnSkip);
            leftPanel.Controls.Add(btnNext);
            leftPanel.Controls.Add(btnFlipBoard);
            leftPanel.Controls.Add(btnRetry);
            leftPanel.Controls.Add(btnAnalyze);
            leftPanel.Controls.Add(lblStatus);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(8, 8);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(543, 634);
            leftPanel.TabIndex = 0;
            leftPanel.Resize += LeftPanel_Resize;

            //
            // boardControl
            //
            boardControl.InteractionEnabled = true;
            boardControl.Location = new Point(10, 10);
            boardControl.Name = "boardControl";
            boardControl.Size = new Size(480, 480);
            boardControl.TabIndex = 0;
            boardControl.MoveMade += BoardControl_MoveMade;

            //
            // lblTurn
            //
            lblTurn.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblTurn.Location = new Point(10, 500);
            lblTurn.Name = "lblTurn";
            lblTurn.Size = new Size(200, 25);
            lblTurn.TabIndex = 1;
            lblTurn.Text = "White to move";

            //
            // btnHint
            //
            btnHint.FlatStyle = FlatStyle.Flat;
            btnHint.Font = new Font("Segoe UI", 9F);
            btnHint.Location = new Point(10, 530);
            btnHint.Name = "btnHint";
            btnHint.Size = new Size(90, 30);
            btnHint.TabIndex = 2;
            btnHint.Text = "Hint (0/3)";
            btnHint.Click += BtnHint_Click;

            //
            // btnRetry
            //
            btnRetry.FlatStyle = FlatStyle.Flat;
            btnRetry.Font = new Font("Segoe UI", 9F);
            btnRetry.Location = new Point(105, 530);
            btnRetry.Name = "btnRetry";
            btnRetry.Size = new Size(70, 30);
            btnRetry.TabIndex = 3;
            btnRetry.Text = "Retry";
            btnRetry.Visible = false;
            btnRetry.Click += BtnRetry_Click;

            //
            // btnSkip
            //
            btnSkip.FlatStyle = FlatStyle.Flat;
            btnSkip.Font = new Font("Segoe UI", 9F);
            btnSkip.Location = new Point(180, 530);
            btnSkip.Name = "btnSkip";
            btnSkip.Size = new Size(70, 30);
            btnSkip.TabIndex = 4;
            btnSkip.Text = "Skip";
            btnSkip.Click += BtnSkip_Click;

            //
            // btnNext
            //
            btnNext.FlatStyle = FlatStyle.Flat;
            btnNext.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnNext.Location = new Point(255, 530);
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(100, 30);
            btnNext.TabIndex = 5;
            btnNext.Text = "Next Puzzle";
            btnNext.Visible = false;
            btnNext.Click += BtnNext_Click;

            //
            // btnFlipBoard
            //
            btnFlipBoard.FlatStyle = FlatStyle.Flat;
            btnFlipBoard.Font = new Font("Segoe UI", 9F);
            btnFlipBoard.Location = new Point(360, 530);
            btnFlipBoard.Name = "btnFlipBoard";
            btnFlipBoard.Size = new Size(50, 30);
            btnFlipBoard.TabIndex = 6;
            btnFlipBoard.Text = "Flip";
            btnFlipBoard.Click += BtnFlipBoard_Click;

            //
            // btnAnalyze
            //
            btnAnalyze.FlatStyle = FlatStyle.Flat;
            btnAnalyze.Font = new Font("Segoe UI", 9F);
            btnAnalyze.Location = new Point(415, 530);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(75, 30);
            btnAnalyze.TabIndex = 8;
            btnAnalyze.Text = "Analyze";
            btnAnalyze.Visible = false;
            btnAnalyze.Click += BtnAnalyze_Click;

            //
            // lblStatus
            //
            lblStatus.Font = new Font("Segoe UI", 9F);
            lblStatus.Location = new Point(10, 570);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(480, 25);
            lblStatus.TabIndex = 7;
            lblStatus.Text = "Loading puzzles...";

            //
            // rightPanel
            //
            rightPanel.AutoScroll = true;
            rightPanel.Controls.Add(grpPuzzleInfo);
            rightPanel.Controls.Add(grpFeedback);
            rightPanel.Controls.Add(grpFilters);
            rightPanel.Controls.Add(grpStats);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(554, 8);
            rightPanel.Name = "rightPanel";
            rightPanel.Size = new Size(338, 634);
            rightPanel.TabIndex = 1;

            //
            // grpPuzzleInfo
            //
            grpPuzzleInfo.Controls.Add(lblPuzzleRating);
            grpPuzzleInfo.Controls.Add(lblPuzzleThemes);
            grpPuzzleInfo.Controls.Add(lblPuzzleProgress);
            grpPuzzleInfo.Dock = DockStyle.Top;
            grpPuzzleInfo.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpPuzzleInfo.Location = new Point(0, 0);
            grpPuzzleInfo.Name = "grpPuzzleInfo";
            grpPuzzleInfo.Padding = new Padding(8, 4, 8, 4);
            grpPuzzleInfo.Size = new Size(338, 100);
            grpPuzzleInfo.TabIndex = 0;
            grpPuzzleInfo.TabStop = false;
            grpPuzzleInfo.Text = "Puzzle Info";

            //
            // lblPuzzleRating
            //
            lblPuzzleRating.Font = new Font("Segoe UI", 10F);
            lblPuzzleRating.Location = new Point(10, 22);
            lblPuzzleRating.Name = "lblPuzzleRating";
            lblPuzzleRating.Size = new Size(310, 22);
            lblPuzzleRating.TabIndex = 0;
            lblPuzzleRating.Text = "Rating: —";

            //
            // lblPuzzleThemes
            //
            lblPuzzleThemes.Font = new Font("Segoe UI", 9F);
            lblPuzzleThemes.Location = new Point(10, 46);
            lblPuzzleThemes.Name = "lblPuzzleThemes";
            lblPuzzleThemes.Size = new Size(310, 22);
            lblPuzzleThemes.TabIndex = 1;
            lblPuzzleThemes.Text = "Themes: —";

            //
            // lblPuzzleProgress
            //
            lblPuzzleProgress.Font = new Font("Segoe UI", 9F);
            lblPuzzleProgress.Location = new Point(10, 70);
            lblPuzzleProgress.Name = "lblPuzzleProgress";
            lblPuzzleProgress.Size = new Size(310, 22);
            lblPuzzleProgress.TabIndex = 2;
            lblPuzzleProgress.Text = "Move — of —";

            //
            // grpFeedback
            //
            grpFeedback.Controls.Add(lblFeedback);
            grpFeedback.Dock = DockStyle.Top;
            grpFeedback.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpFeedback.Location = new Point(0, 100);
            grpFeedback.Name = "grpFeedback";
            grpFeedback.Padding = new Padding(8, 4, 8, 4);
            grpFeedback.Size = new Size(338, 100);
            grpFeedback.TabIndex = 1;
            grpFeedback.TabStop = false;
            grpFeedback.Text = "Feedback";

            //
            // lblFeedback
            //
            lblFeedback.Font = new Font("Segoe UI", 10F);
            lblFeedback.Location = new Point(10, 22);
            lblFeedback.Name = "lblFeedback";
            lblFeedback.Size = new Size(310, 68);
            lblFeedback.TabIndex = 0;
            lblFeedback.Text = "Waiting for puzzle...";

            //
            // grpFilters
            //
            grpFilters.Controls.Add(lblMinRating);
            grpFilters.Controls.Add(numMinRating);
            grpFilters.Controls.Add(lblMaxRating);
            grpFilters.Controls.Add(numMaxRating);
            grpFilters.Controls.Add(lblThemeFilter);
            grpFilters.Controls.Add(cmbThemeFilter);
            grpFilters.Dock = DockStyle.Top;
            grpFilters.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpFilters.Location = new Point(0, 200);
            grpFilters.Name = "grpFilters";
            grpFilters.Padding = new Padding(8, 4, 8, 4);
            grpFilters.Size = new Size(338, 115);
            grpFilters.TabIndex = 2;
            grpFilters.TabStop = false;
            grpFilters.Text = "Filters";

            //
            // lblMinRating
            //
            lblMinRating.Font = new Font("Segoe UI", 9F);
            lblMinRating.Location = new Point(10, 24);
            lblMinRating.Name = "lblMinRating";
            lblMinRating.Size = new Size(70, 20);
            lblMinRating.TabIndex = 0;
            lblMinRating.Text = "Min Rating:";

            //
            // numMinRating
            //
            numMinRating.Font = new Font("Segoe UI", 9F);
            numMinRating.Increment = 50;
            numMinRating.Location = new Point(85, 22);
            numMinRating.Maximum = 3300;
            numMinRating.Minimum = 400;
            numMinRating.Name = "numMinRating";
            numMinRating.Size = new Size(70, 23);
            numMinRating.TabIndex = 1;
            numMinRating.Value = 800;

            //
            // lblMaxRating
            //
            lblMaxRating.Font = new Font("Segoe UI", 9F);
            lblMaxRating.Location = new Point(170, 24);
            lblMaxRating.Name = "lblMaxRating";
            lblMaxRating.Size = new Size(73, 20);
            lblMaxRating.TabIndex = 2;
            lblMaxRating.Text = "Max Rating:";

            //
            // numMaxRating
            //
            numMaxRating.Font = new Font("Segoe UI", 9F);
            numMaxRating.Increment = 50;
            numMaxRating.Location = new Point(248, 22);
            numMaxRating.Maximum = 3300;
            numMaxRating.Minimum = 400;
            numMaxRating.Name = "numMaxRating";
            numMaxRating.Size = new Size(70, 23);
            numMaxRating.TabIndex = 3;
            numMaxRating.Value = 2000;

            //
            // lblThemeFilter
            //
            lblThemeFilter.Font = new Font("Segoe UI", 9F);
            lblThemeFilter.Location = new Point(10, 56);
            lblThemeFilter.Name = "lblThemeFilter";
            lblThemeFilter.Size = new Size(50, 20);
            lblThemeFilter.TabIndex = 4;
            lblThemeFilter.Text = "Theme:";

            //
            // cmbThemeFilter
            //
            cmbThemeFilter.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbThemeFilter.Font = new Font("Segoe UI", 9F);
            cmbThemeFilter.Location = new Point(65, 53);
            cmbThemeFilter.Name = "cmbThemeFilter";
            cmbThemeFilter.Size = new Size(253, 23);
            cmbThemeFilter.TabIndex = 5;

            //
            // grpStats
            //
            grpStats.Controls.Add(btnResetStats);
            grpStats.Controls.Add(lblStatsRating);
            grpStats.Controls.Add(lblStatsSolved);
            grpStats.Controls.Add(lblStatsStreak);
            grpStats.Controls.Add(lblStatsHints);
            grpStats.Controls.Add(lblStatsAccuracy);
            grpStats.Dock = DockStyle.Top;
            grpStats.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            grpStats.Location = new Point(0, 315);
            grpStats.Name = "grpStats";
            grpStats.Padding = new Padding(8, 4, 8, 4);
            grpStats.Size = new Size(338, 150);
            grpStats.TabIndex = 3;
            grpStats.TabStop = false;
            grpStats.Text = "Statistics";

            //
            // btnResetStats
            //
            btnResetStats.FlatStyle = FlatStyle.Flat;
            btnResetStats.Font = new Font("Segoe UI", 8F);
            btnResetStats.Location = new Point(258, 0);
            btnResetStats.Name = "btnResetStats";
            btnResetStats.Size = new Size(60, 20);
            btnResetStats.TabIndex = 5;
            btnResetStats.Text = "Reset";
            btnResetStats.Click += BtnResetStats_Click;

            //
            // lblStatsRating
            //
            lblStatsRating.Font = new Font("Segoe UI", 10F);
            lblStatsRating.Location = new Point(10, 24);
            lblStatsRating.Name = "lblStatsRating";
            lblStatsRating.Size = new Size(310, 22);
            lblStatsRating.TabIndex = 0;
            lblStatsRating.Text = "Puzzle Rating: 1200";

            //
            // lblStatsSolved
            //
            lblStatsSolved.Font = new Font("Segoe UI", 9F);
            lblStatsSolved.Location = new Point(10, 48);
            lblStatsSolved.Name = "lblStatsSolved";
            lblStatsSolved.Size = new Size(310, 22);
            lblStatsSolved.TabIndex = 1;
            lblStatsSolved.Text = "Solved: 0 / 0";

            //
            // lblStatsAccuracy
            //
            lblStatsAccuracy.Font = new Font("Segoe UI", 9F);
            lblStatsAccuracy.Location = new Point(10, 72);
            lblStatsAccuracy.Name = "lblStatsAccuracy";
            lblStatsAccuracy.Size = new Size(310, 22);
            lblStatsAccuracy.TabIndex = 2;
            lblStatsAccuracy.Text = "Accuracy: —";

            //
            // lblStatsStreak
            //
            lblStatsStreak.Font = new Font("Segoe UI", 9F);
            lblStatsStreak.Location = new Point(10, 96);
            lblStatsStreak.Name = "lblStatsStreak";
            lblStatsStreak.Size = new Size(310, 22);
            lblStatsStreak.TabIndex = 3;
            lblStatsStreak.Text = "Streak: 0 (Best: 0)";

            //
            // lblStatsHints
            //
            lblStatsHints.Font = new Font("Segoe UI", 9F);
            lblStatsHints.Location = new Point(10, 120);
            lblStatsHints.Name = "lblStatsHints";
            lblStatsHints.Size = new Size(310, 22);
            lblStatsHints.TabIndex = 4;
            lblStatsHints.Text = "Hints used: 0";

            //
            // PuzzleForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 650);
            Controls.Add(mainLayout);
            MinimumSize = new Size(750, 550);
            Name = "PuzzleForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Puzzle Mode — chessdroid";
            FormClosing += PuzzleForm_FormClosing;

            mainLayout.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            grpPuzzleInfo.ResumeLayout(false);
            grpFeedback.ResumeLayout(false);
            grpFilters.ResumeLayout(false);
            grpStats.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numMinRating).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMaxRating).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel mainLayout;
        private Panel leftPanel;
        private ChessBoardControl boardControl;
        private Label lblTurn;
        private Button btnHint;
        private Button btnSkip;
        private Button btnNext;
        private Button btnFlipBoard;
        private Button btnRetry;
        private Button btnAnalyze;
        private Label lblStatus;
        private Panel rightPanel;
        private GroupBox grpPuzzleInfo;
        private Label lblPuzzleRating;
        private Label lblPuzzleThemes;
        private Label lblPuzzleProgress;
        private GroupBox grpFeedback;
        private Label lblFeedback;
        private GroupBox grpFilters;
        private Label lblMinRating;
        private NumericUpDown numMinRating;
        private Label lblMaxRating;
        private NumericUpDown numMaxRating;
        private Label lblThemeFilter;
        private ComboBox cmbThemeFilter;
        private GroupBox grpStats;
        private Button btnResetStats;
        private Label lblStatsRating;
        private Label lblStatsSolved;
        private Label lblStatsStreak;
        private Label lblStatsHints;
        private Label lblStatsAccuracy;
    }
}
