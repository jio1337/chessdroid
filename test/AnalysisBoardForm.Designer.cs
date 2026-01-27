using ChessDroid.Controls;

namespace ChessDroid
{
    partial class AnalysisBoardForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            mainLayout = new TableLayoutPanel();
            leftPanel = new Panel();
            boardControl = new ChessBoardControl();
            lblTurn = new Label();
            btnNewGame = new Button();
            btnFlipBoard = new Button();
            btnTakeBack = new Button();
            btnPrevMove = new Button();
            btnNextMove = new Button();
            btnAnalyze = new Button();
            chkAutoAnalyze = new CheckBox();
            lblFen = new Label();
            txtFen = new TextBox();
            btnLoadFen = new Button();
            btnCopyFen = new Button();
            lblStatus = new Label();
            middlePanel = new Panel();
            moveListBox = new ListBox();
            lblMoves = new Label();
            rightPanel = new Panel();
            analysisOutput = new RichTextBox();
            lblAnalysis = new Label();
            mainLayout.SuspendLayout();
            leftPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            rightPanel.SuspendLayout();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 3;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            mainLayout.Controls.Add(leftPanel, 0, 0);
            mainLayout.Controls.Add(middlePanel, 1, 0);
            mainLayout.Controls.Add(rightPanel, 2, 0);
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Location = new Point(0, 0);
            mainLayout.Name = "mainLayout";
            mainLayout.Padding = new Padding(5);
            mainLayout.RowCount = 1;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Size = new Size(1068, 646);
            mainLayout.TabIndex = 0;
            // 
            // leftPanel
            // 
            leftPanel.Controls.Add(boardControl);
            leftPanel.Controls.Add(lblTurn);
            leftPanel.Controls.Add(btnNewGame);
            leftPanel.Controls.Add(btnFlipBoard);
            leftPanel.Controls.Add(btnTakeBack);
            leftPanel.Controls.Add(btnPrevMove);
            leftPanel.Controls.Add(btnNextMove);
            leftPanel.Controls.Add(btnAnalyze);
            leftPanel.Controls.Add(chkAutoAnalyze);
            leftPanel.Controls.Add(lblFen);
            leftPanel.Controls.Add(txtFen);
            leftPanel.Controls.Add(btnLoadFen);
            leftPanel.Controls.Add(btnCopyFen);
            leftPanel.Controls.Add(lblStatus);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(8, 8);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(498, 630);
            leftPanel.TabIndex = 0;
            leftPanel.Resize += LeftPanel_Resize;
            // 
            // boardControl
            // 
            boardControl.Location = new Point(10, 10);
            boardControl.Name = "boardControl";
            boardControl.Size = new Size(480, 480);
            boardControl.TabIndex = 0;
            boardControl.MoveMade += BoardControl_MoveMade;
            boardControl.BoardChanged += BoardControl_BoardChanged;
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
            // btnNewGame
            // 
            btnNewGame.FlatStyle = FlatStyle.Flat;
            btnNewGame.Font = new Font("Courier New", 8.25F);
            btnNewGame.Location = new Point(10, 530);
            btnNewGame.Name = "btnNewGame";
            btnNewGame.Size = new Size(90, 28);
            btnNewGame.TabIndex = 2;
            btnNewGame.Text = "New Game";
            btnNewGame.Click += BtnNewGame_Click;
            // 
            // btnFlipBoard
            // 
            btnFlipBoard.FlatStyle = FlatStyle.Flat;
            btnFlipBoard.Font = new Font("Courier New", 8.25F);
            btnFlipBoard.Location = new Point(105, 530);
            btnFlipBoard.Name = "btnFlipBoard";
            btnFlipBoard.Size = new Size(90, 28);
            btnFlipBoard.TabIndex = 3;
            btnFlipBoard.Text = "Flip Board";
            btnFlipBoard.Click += BtnFlipBoard_Click;
            // 
            // btnTakeBack
            // 
            btnTakeBack.FlatStyle = FlatStyle.Flat;
            btnTakeBack.Font = new Font("Courier New", 8.25F);
            btnTakeBack.Location = new Point(200, 530);
            btnTakeBack.Name = "btnTakeBack";
            btnTakeBack.Size = new Size(90, 28);
            btnTakeBack.TabIndex = 4;
            btnTakeBack.Text = "Take Back";
            btnTakeBack.Click += BtnTakeBack_Click;
            // 
            // btnPrevMove
            // 
            btnPrevMove.FlatStyle = FlatStyle.Flat;
            btnPrevMove.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnPrevMove.Location = new Point(295, 530);
            btnPrevMove.Name = "btnPrevMove";
            btnPrevMove.Size = new Size(35, 28);
            btnPrevMove.TabIndex = 12;
            btnPrevMove.Text = "◀";
            btnPrevMove.Click += BtnPrevMove_Click;
            // 
            // btnNextMove
            // 
            btnNextMove.FlatStyle = FlatStyle.Flat;
            btnNextMove.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnNextMove.Location = new Point(335, 530);
            btnNextMove.Name = "btnNextMove";
            btnNextMove.Size = new Size(35, 28);
            btnNextMove.TabIndex = 13;
            btnNextMove.Text = "▶";
            btnNextMove.Click += BtnNextMove_Click;
            // 
            // btnAnalyze
            // 
            btnAnalyze.FlatStyle = FlatStyle.Flat;
            btnAnalyze.Font = new Font("Courier New", 8.25F);
            btnAnalyze.ForeColor = Color.White;
            btnAnalyze.Location = new Point(377, 530);
            btnAnalyze.Name = "btnAnalyze";
            btnAnalyze.Size = new Size(110, 28);
            btnAnalyze.TabIndex = 5;
            btnAnalyze.Text = "Analyze (F2)";
            btnAnalyze.Click += BtnAnalyze_Click;
            // 
            // chkAutoAnalyze
            // 
            chkAutoAnalyze.Checked = true;
            chkAutoAnalyze.CheckState = CheckState.Checked;
            chkAutoAnalyze.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkAutoAnalyze.Location = new Point(407, 603);
            chkAutoAnalyze.Name = "chkAutoAnalyze";
            chkAutoAnalyze.Size = new Size(55, 20);
            chkAutoAnalyze.TabIndex = 6;
            chkAutoAnalyze.Text = "Auto";
            chkAutoAnalyze.CheckedChanged += ChkAutoAnalyze_CheckedChanged;
            // 
            // lblFen
            // 
            lblFen.Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblFen.Location = new Point(10, 574);
            lblFen.Name = "lblFen";
            lblFen.Size = new Size(35, 23);
            lblFen.TabIndex = 7;
            lblFen.Text = "FEN:";
            // 
            // txtFen
            // 
            txtFen.Font = new Font("Consolas", 9F);
            txtFen.Location = new Point(45, 571);
            txtFen.Name = "txtFen";
            txtFen.Size = new Size(310, 22);
            txtFen.TabIndex = 8;
            // 
            // btnLoadFen
            // 
            btnLoadFen.FlatStyle = FlatStyle.Flat;
            btnLoadFen.Font = new Font("Courier New", 8.25F);
            btnLoadFen.Location = new Point(360, 569);
            btnLoadFen.Name = "btnLoadFen";
            btnLoadFen.Size = new Size(55, 28);
            btnLoadFen.TabIndex = 9;
            btnLoadFen.Text = "Load";
            btnLoadFen.Click += BtnLoadFen_Click;
            // 
            // btnCopyFen
            // 
            btnCopyFen.FlatStyle = FlatStyle.Flat;
            btnCopyFen.Font = new Font("Courier New", 8.25F);
            btnCopyFen.Location = new Point(420, 569);
            btnCopyFen.Name = "btnCopyFen";
            btnCopyFen.Size = new Size(55, 28);
            btnCopyFen.TabIndex = 10;
            btnCopyFen.Text = "Copy";
            btnCopyFen.Click += BtnCopyFen_Click;
            // 
            // lblStatus
            // 
            lblStatus.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblStatus.Location = new Point(10, 603);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(470, 20);
            lblStatus.TabIndex = 11;
            lblStatus.Text = "Ready";
            // 
            // middlePanel
            // 
            middlePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            middlePanel.Controls.Add(moveListBox);
            middlePanel.Controls.Add(lblMoves);
            middlePanel.Location = new Point(512, 8);
            middlePanel.Name = "middlePanel";
            middlePanel.Padding = new Padding(5);
            middlePanel.Size = new Size(134, 503);
            middlePanel.TabIndex = 1;
            // 
            // moveListBox
            // 
            moveListBox.BorderStyle = BorderStyle.FixedSingle;
            moveListBox.Dock = DockStyle.Fill;
            moveListBox.Font = new Font("Consolas", 10F);
            moveListBox.ItemHeight = 15;
            moveListBox.Location = new Point(5, 30);
            moveListBox.Name = "moveListBox";
            moveListBox.Size = new Size(124, 468);
            moveListBox.TabIndex = 1;
            moveListBox.SelectedIndexChanged += MoveListBox_SelectedIndexChanged;
            // 
            // lblMoves
            // 
            lblMoves.Dock = DockStyle.Top;
            lblMoves.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblMoves.Location = new Point(5, 5);
            lblMoves.Name = "lblMoves";
            lblMoves.Size = new Size(124, 25);
            lblMoves.TabIndex = 0;
            lblMoves.Text = "Moves";
            // 
            // rightPanel
            // 
            rightPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            rightPanel.Controls.Add(analysisOutput);
            rightPanel.Controls.Add(lblAnalysis);
            rightPanel.Location = new Point(652, 8);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(5);
            rightPanel.Size = new Size(408, 503);
            rightPanel.TabIndex = 2;
            // 
            // analysisOutput
            // 
            analysisOutput.BorderStyle = BorderStyle.FixedSingle;
            analysisOutput.Dock = DockStyle.Fill;
            analysisOutput.Font = new Font("Consolas", 10F);
            analysisOutput.Location = new Point(5, 25);
            analysisOutput.Name = "analysisOutput";
            analysisOutput.ReadOnly = true;
            analysisOutput.ScrollBars = RichTextBoxScrollBars.Vertical;
            analysisOutput.Size = new Size(398, 473);
            analysisOutput.TabIndex = 1;
            analysisOutput.Text = "";
            // 
            // lblAnalysis
            // 
            lblAnalysis.Dock = DockStyle.Top;
            lblAnalysis.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAnalysis.Location = new Point(5, 5);
            lblAnalysis.Name = "lblAnalysis";
            lblAnalysis.Size = new Size(398, 20);
            lblAnalysis.TabIndex = 0;
            lblAnalysis.Text = "Engine Analysis";
            // 
            // AnalysisBoardForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1068, 646);
            Controls.Add(mainLayout);
            KeyPreview = true;
            MinimumSize = new Size(1000, 600);
            Name = "AnalysisBoardForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "chessdroid://analysis";
            KeyDown += AnalysisBoardForm_KeyDown;
            mainLayout.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            leftPanel.PerformLayout();
            middlePanel.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // Layout panels
        private TableLayoutPanel mainLayout;
        private Panel leftPanel;
        private Panel middlePanel;
        private Panel rightPanel;

        // Left panel - Board and controls
        private ChessBoardControl boardControl;
        private Label lblTurn;
        private Button btnNewGame;
        private Button btnFlipBoard;
        private Button btnTakeBack;
        private Button btnPrevMove;
        private Button btnNextMove;
        private Button btnAnalyze;
        private CheckBox chkAutoAnalyze;
        private Label lblFen;
        private TextBox txtFen;
        private Button btnLoadFen;
        private Button btnCopyFen;
        private Label lblStatus;

        // Middle panel - Move list
        private Label lblMoves;
        private ListBox moveListBox;

        // Right panel - Analysis
        private Label lblAnalysis;
        private RichTextBox analysisOutput;
    }
}
