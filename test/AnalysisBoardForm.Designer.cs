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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AnalysisBoardForm));
            mainLayout = new TableLayoutPanel();
            leftPanel = new Panel();
            evalBar = new EvalBarControl();
            boardControl = new ChessBoardControl();
            lblTurn = new Label();
            lblPieces = new Label();
            cmbPieces = new ComboBox();
            btnSettings = new Button();
            btnNewGame = new Button();
            btnFlipBoard = new Button();
            btnTakeBack = new Button();
            btnPrevMove = new Button();
            btnNextMove = new Button();

            lblFen = new Label();
            txtFen = new TextBox();
            btnLoadFen = new Button();
            btnCopyFen = new Button();
            lblStatus = new Label();
            middlePanel = new Panel();
            moveListBox = new ListBox();
            pnlPgnButtons = new Panel();
            btnClassifyMoves = new Button();
            btnExportPgn = new Button();
            btnImportPgn = new Button();
            lblMoves = new Label();
            rightPanel = new Panel();
            analysisOutput = new RichTextBox();
            grpEngineMatch = new GroupBox();
            lblWhiteEngine = new Label();
            cmbWhiteEngine = new ComboBox();
            lblBlackEngine = new Label();
            cmbBlackEngine = new ComboBox();
            lblTimeControl = new Label();
            cmbTimeControlType = new ComboBox();
            pnlTimeParams = new Panel();
            lblDepth = new Label();
            numDepth = new NumericUpDown();
            lblMoveTime = new Label();
            numMoveTime = new NumericUpDown();
            lblTotalTime = new Label();
            numTotalTime = new NumericUpDown();
            lblIncrement = new Label();
            numIncrement = new NumericUpDown();
            lblWhiteClock = new Label();
            lblBlackClock = new Label();
            btnStartMatch = new Button();
            btnStopMatch = new Button();
            chkFromPosition = new CheckBox();
            lblAnalysis = new Label();
            mainLayout.SuspendLayout();
            leftPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            pnlPgnButtons.SuspendLayout();
            rightPanel.SuspendLayout();
            grpEngineMatch.SuspendLayout();
            pnlTimeParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDepth).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTotalTime).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numIncrement).BeginInit();
            SuspendLayout();
            // 
            // mainLayout
            // 
            mainLayout.ColumnCount = 3;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
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
            leftPanel.Controls.Add(evalBar);
            leftPanel.Controls.Add(boardControl);
            leftPanel.Controls.Add(lblTurn);
            leftPanel.Controls.Add(lblPieces);
            leftPanel.Controls.Add(cmbPieces);
            leftPanel.Controls.Add(btnSettings);
            leftPanel.Controls.Add(btnNewGame);
            leftPanel.Controls.Add(btnFlipBoard);
            leftPanel.Controls.Add(btnTakeBack);
            leftPanel.Controls.Add(btnPrevMove);
            leftPanel.Controls.Add(btnNextMove);

            leftPanel.Controls.Add(lblFen);
            leftPanel.Controls.Add(txtFen);
            leftPanel.Controls.Add(btnLoadFen);
            leftPanel.Controls.Add(btnCopyFen);
            leftPanel.Controls.Add(lblStatus);
            leftPanel.Dock = DockStyle.Fill;
            leftPanel.Location = new Point(8, 8);
            leftPanel.Name = "leftPanel";
            leftPanel.Size = new Size(569, 630);
            leftPanel.TabIndex = 0;
            leftPanel.Resize += LeftPanel_Resize;
            // 
            // evalBar
            // 
            evalBar.Location = new Point(10, 10);
            evalBar.Name = "evalBar";
            evalBar.Size = new Size(24, 480);
            evalBar.TabIndex = 20;
            // 
            // boardControl
            // 
            boardControl.InteractionEnabled = true;
            boardControl.Location = new Point(38, 10);
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
            lblTurn.Size = new Size(123, 25);
            lblTurn.TabIndex = 1;
            lblTurn.Text = "White to move";
            // 
            // lblPieces
            // 
            lblPieces.Font = new Font("Courier New", 8.25F);
            lblPieces.Location = new Point(407, 537);
            lblPieces.Name = "lblPieces";
            lblPieces.Size = new Size(50, 20);
            lblPieces.TabIndex = 21;
            lblPieces.Text = "Pieces:";
            // 
            // cmbPieces
            // 
            cmbPieces.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPieces.Font = new Font("Courier New", 8.25F);
            cmbPieces.Location = new Point(462, 534);
            cmbPieces.Name = "cmbPieces";
            cmbPieces.Size = new Size(93, 22);
            cmbPieces.TabIndex = 22;
            cmbPieces.SelectedIndexChanged += CmbPieces_SelectedIndexChanged;
            // 
            // btnSettings
            // 
            btnSettings.FlatStyle = FlatStyle.Flat;
            btnSettings.Font = new Font("Segoe UI", 10F);
            btnSettings.Location = new Point(10, 530);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(28, 28);
            btnSettings.TabIndex = 23;
            btnSettings.Text = "⚙";
            btnSettings.Click += BtnSettings_Click;
            // 
            // btnNewGame
            // 
            btnNewGame.FlatStyle = FlatStyle.Flat;
            btnNewGame.Font = new Font("Courier New", 8.25F);
            btnNewGame.Location = new Point(43, 530);
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
            btnFlipBoard.Location = new Point(138, 530);
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
            btnTakeBack.Location = new Point(233, 530);
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
            btnPrevMove.Location = new Point(328, 530);
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
            btnNextMove.Location = new Point(368, 530);
            btnNextMove.Name = "btnNextMove";
            btnNextMove.Size = new Size(35, 28);
            btnNextMove.TabIndex = 13;
            btnNextMove.Text = "▶";
            btnNextMove.Click += BtnNextMove_Click;

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
            lblStatus.Size = new Size(391, 20);
            lblStatus.TabIndex = 11;
            lblStatus.Text = "Ready";
            // 
            // middlePanel
            // 
            middlePanel.Controls.Add(moveListBox);
            middlePanel.Controls.Add(pnlPgnButtons);
            middlePanel.Controls.Add(lblMoves);
            middlePanel.Dock = DockStyle.Fill;
            middlePanel.Location = new Point(583, 8);
            middlePanel.Name = "middlePanel";
            middlePanel.Padding = new Padding(5);
            middlePanel.Size = new Size(124, 630);
            middlePanel.TabIndex = 1;
            // 
            // moveListBox
            // 
            moveListBox.BorderStyle = BorderStyle.FixedSingle;
            moveListBox.Dock = DockStyle.Fill;
            moveListBox.DrawMode = DrawMode.OwnerDrawFixed;
            moveListBox.Font = new Font("Consolas", 10F);
            moveListBox.ItemHeight = 18;
            moveListBox.Location = new Point(5, 30);
            moveListBox.Name = "moveListBox";
            moveListBox.Size = new Size(114, 500);
            moveListBox.TabIndex = 1;
            moveListBox.DrawItem += MoveListBox_DrawItem;
            moveListBox.SelectedIndexChanged += MoveListBox_SelectedIndexChanged;
            // 
            // pnlPgnButtons
            // 
            pnlPgnButtons.Controls.Add(btnClassifyMoves);
            pnlPgnButtons.Controls.Add(btnExportPgn);
            pnlPgnButtons.Controls.Add(btnImportPgn);
            pnlPgnButtons.Dock = DockStyle.Bottom;
            pnlPgnButtons.Location = new Point(5, 530);
            pnlPgnButtons.Name = "pnlPgnButtons";
            pnlPgnButtons.Size = new Size(114, 95);
            pnlPgnButtons.TabIndex = 2;
            // 
            // btnClassifyMoves
            // 
            btnClassifyMoves.Dock = DockStyle.Top;
            btnClassifyMoves.FlatStyle = FlatStyle.Flat;
            btnClassifyMoves.Font = new Font("Courier New", 8.25F);
            btnClassifyMoves.Location = new Point(0, 30);
            btnClassifyMoves.Name = "btnClassifyMoves";
            btnClassifyMoves.Size = new Size(114, 37);
            btnClassifyMoves.TabIndex = 0;
            btnClassifyMoves.Text = "Classify";
            btnClassifyMoves.Click += BtnClassifyMoves_Click;
            // 
            // btnExportPgn
            // 
            btnExportPgn.Dock = DockStyle.Top;
            btnExportPgn.FlatStyle = FlatStyle.Flat;
            btnExportPgn.Font = new Font("Courier New", 8.25F);
            btnExportPgn.Location = new Point(0, 0);
            btnExportPgn.Name = "btnExportPgn";
            btnExportPgn.Size = new Size(114, 30);
            btnExportPgn.TabIndex = 1;
            btnExportPgn.Text = "Export PGN";
            btnExportPgn.Click += BtnExportPgn_Click;
            // 
            // btnImportPgn
            // 
            btnImportPgn.Dock = DockStyle.Bottom;
            btnImportPgn.FlatStyle = FlatStyle.Flat;
            btnImportPgn.Font = new Font("Courier New", 8.25F);
            btnImportPgn.Location = new Point(0, 65);
            btnImportPgn.Name = "btnImportPgn";
            btnImportPgn.Size = new Size(114, 30);
            btnImportPgn.TabIndex = 2;
            btnImportPgn.Text = "Import PGN";
            btnImportPgn.Click += BtnImportPgn_Click;
            // 
            // lblMoves
            // 
            lblMoves.Dock = DockStyle.Top;
            lblMoves.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblMoves.Location = new Point(5, 5);
            lblMoves.Name = "lblMoves";
            lblMoves.Size = new Size(114, 25);
            lblMoves.TabIndex = 0;
            lblMoves.Text = "Moves";
            // 
            // rightPanel
            // 
            rightPanel.Controls.Add(analysisOutput);
            rightPanel.Controls.Add(grpEngineMatch);
            rightPanel.Controls.Add(lblAnalysis);
            rightPanel.Dock = DockStyle.Fill;
            rightPanel.Location = new Point(713, 8);
            rightPanel.Name = "rightPanel";
            rightPanel.Padding = new Padding(5);
            rightPanel.Size = new Size(347, 630);
            rightPanel.TabIndex = 2;
            // 
            // analysisOutput
            // 
            analysisOutput.BorderStyle = BorderStyle.FixedSingle;
            analysisOutput.Dock = DockStyle.Fill;
            analysisOutput.Font = new Font("Consolas", 10F);
            analysisOutput.Location = new Point(5, 265);
            analysisOutput.Name = "analysisOutput";
            analysisOutput.ReadOnly = true;
            analysisOutput.ScrollBars = RichTextBoxScrollBars.Vertical;
            analysisOutput.Size = new Size(337, 360);
            analysisOutput.TabIndex = 1;
            analysisOutput.Text = "";
            // 
            // grpEngineMatch
            // 
            grpEngineMatch.Controls.Add(lblWhiteEngine);
            grpEngineMatch.Controls.Add(cmbWhiteEngine);
            grpEngineMatch.Controls.Add(lblBlackEngine);
            grpEngineMatch.Controls.Add(cmbBlackEngine);
            grpEngineMatch.Controls.Add(lblTimeControl);
            grpEngineMatch.Controls.Add(cmbTimeControlType);
            grpEngineMatch.Controls.Add(pnlTimeParams);
            grpEngineMatch.Controls.Add(lblWhiteClock);
            grpEngineMatch.Controls.Add(lblBlackClock);
            grpEngineMatch.Controls.Add(btnStartMatch);
            grpEngineMatch.Controls.Add(btnStopMatch);
            grpEngineMatch.Controls.Add(chkFromPosition);
            grpEngineMatch.Dock = DockStyle.Top;
            grpEngineMatch.Font = new Font("Courier New", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            grpEngineMatch.Location = new Point(5, 25);
            grpEngineMatch.Name = "grpEngineMatch";
            grpEngineMatch.Size = new Size(337, 240);
            grpEngineMatch.TabIndex = 2;
            grpEngineMatch.TabStop = false;
            grpEngineMatch.Text = "Engine Match";
            grpEngineMatch.Resize += GrpEngineMatch_Resize;
            // 
            // lblWhiteEngine
            // 
            lblWhiteEngine.Font = new Font("Courier New", 9F);
            lblWhiteEngine.Location = new Point(10, 22);
            lblWhiteEngine.Name = "lblWhiteEngine";
            lblWhiteEngine.Size = new Size(50, 20);
            lblWhiteEngine.TabIndex = 0;
            lblWhiteEngine.Text = "White:";
            // 
            // cmbWhiteEngine
            // 
            cmbWhiteEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbWhiteEngine.Font = new Font("Courier New", 9F);
            cmbWhiteEngine.Location = new Point(65, 19);
            cmbWhiteEngine.Name = "cmbWhiteEngine";
            cmbWhiteEngine.Size = new Size(155, 23);
            cmbWhiteEngine.TabIndex = 1;
            // 
            // lblBlackEngine
            // 
            lblBlackEngine.Font = new Font("Courier New", 9F);
            lblBlackEngine.Location = new Point(10, 50);
            lblBlackEngine.Name = "lblBlackEngine";
            lblBlackEngine.Size = new Size(50, 20);
            lblBlackEngine.TabIndex = 2;
            lblBlackEngine.Text = "Black:";
            // 
            // cmbBlackEngine
            // 
            cmbBlackEngine.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbBlackEngine.Font = new Font("Courier New", 9F);
            cmbBlackEngine.Location = new Point(65, 47);
            cmbBlackEngine.Name = "cmbBlackEngine";
            cmbBlackEngine.Size = new Size(155, 23);
            cmbBlackEngine.TabIndex = 3;
            // 
            // lblTimeControl
            // 
            lblTimeControl.Font = new Font("Courier New", 9F);
            lblTimeControl.Location = new Point(10, 78);
            lblTimeControl.Name = "lblTimeControl";
            lblTimeControl.Size = new Size(50, 20);
            lblTimeControl.TabIndex = 4;
            lblTimeControl.Text = "Time:";
            // 
            // cmbTimeControlType
            // 
            cmbTimeControlType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTimeControlType.Font = new Font("Courier New", 9F);
            cmbTimeControlType.Items.AddRange(new object[] { "Fixed Depth", "Time per Move", "Total + Increment" });
            cmbTimeControlType.Location = new Point(65, 75);
            cmbTimeControlType.Name = "cmbTimeControlType";
            cmbTimeControlType.Size = new Size(155, 23);
            cmbTimeControlType.TabIndex = 5;
            cmbTimeControlType.SelectedIndexChanged += CmbTimeControlType_SelectedIndexChanged;
            // 
            // pnlTimeParams
            // 
            pnlTimeParams.Controls.Add(lblDepth);
            pnlTimeParams.Controls.Add(numDepth);
            pnlTimeParams.Controls.Add(lblMoveTime);
            pnlTimeParams.Controls.Add(numMoveTime);
            pnlTimeParams.Controls.Add(lblTotalTime);
            pnlTimeParams.Controls.Add(numTotalTime);
            pnlTimeParams.Controls.Add(lblIncrement);
            pnlTimeParams.Controls.Add(numIncrement);
            pnlTimeParams.Font = new Font("Segoe UI", 9F);
            pnlTimeParams.Location = new Point(10, 103);
            pnlTimeParams.Name = "pnlTimeParams";
            pnlTimeParams.Size = new Size(321, 30);
            pnlTimeParams.TabIndex = 6;
            // 
            // lblDepth
            // 
            lblDepth.Font = new Font("Courier New", 9F);
            lblDepth.Location = new Point(0, 5);
            lblDepth.Name = "lblDepth";
            lblDepth.Size = new Size(45, 20);
            lblDepth.TabIndex = 0;
            lblDepth.Text = "Depth:";
            // 
            // numDepth
            // 
            numDepth.Font = new Font("Courier New", 9F);
            numDepth.Location = new Point(50, 3);
            numDepth.Maximum = new decimal(new int[] { 30, 0, 0, 0 });
            numDepth.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numDepth.Name = "numDepth";
            numDepth.Size = new Size(55, 21);
            numDepth.TabIndex = 1;
            numDepth.Value = new decimal(new int[] { 15, 0, 0, 0 });
            // 
            // lblMoveTime
            // 
            lblMoveTime.Location = new Point(0, 5);
            lblMoveTime.Name = "lblMoveTime";
            lblMoveTime.Size = new Size(70, 20);
            lblMoveTime.TabIndex = 2;
            lblMoveTime.Text = "ms/move:";
            lblMoveTime.Visible = false;
            // 
            // numMoveTime
            // 
            numMoveTime.Font = new Font("Courier New", 9F);
            numMoveTime.Increment = new decimal(new int[] { 100, 0, 0, 0 });
            numMoveTime.Location = new Point(75, 3);
            numMoveTime.Maximum = new decimal(new int[] { 60000, 0, 0, 0 });
            numMoveTime.Minimum = new decimal(new int[] { 100, 0, 0, 0 });
            numMoveTime.Name = "numMoveTime";
            numMoveTime.Size = new Size(70, 21);
            numMoveTime.TabIndex = 3;
            numMoveTime.Value = new decimal(new int[] { 1000, 0, 0, 0 });
            numMoveTime.Visible = false;
            // 
            // lblTotalTime
            // 
            lblTotalTime.Location = new Point(0, 5);
            lblTotalTime.Name = "lblTotalTime";
            lblTotalTime.Size = new Size(50, 20);
            lblTotalTime.TabIndex = 4;
            lblTotalTime.Text = "Time(s):";
            lblTotalTime.Visible = false;
            // 
            // numTotalTime
            // 
            numTotalTime.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            numTotalTime.Location = new Point(55, 3);
            numTotalTime.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            numTotalTime.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numTotalTime.Name = "numTotalTime";
            numTotalTime.Size = new Size(60, 21);
            numTotalTime.TabIndex = 5;
            numTotalTime.Value = new decimal(new int[] { 300, 0, 0, 0 });
            numTotalTime.Visible = false;
            // 
            // lblIncrement
            // 
            lblIncrement.Font = new Font("Courier New", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblIncrement.Location = new Point(125, 5);
            lblIncrement.Name = "lblIncrement";
            lblIncrement.Size = new Size(40, 20);
            lblIncrement.TabIndex = 6;
            lblIncrement.Text = "Inc(s):";
            lblIncrement.Visible = false;
            // 
            // numIncrement
            // 
            numIncrement.Font = new Font("Courier New", 9F);
            numIncrement.Location = new Point(170, 3);
            numIncrement.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            numIncrement.Name = "numIncrement";
            numIncrement.Size = new Size(50, 21);
            numIncrement.TabIndex = 7;
            numIncrement.Value = new decimal(new int[] { 2, 0, 0, 0 });
            numIncrement.Visible = false;
            // 
            // lblWhiteClock
            // 
            lblWhiteClock.BorderStyle = BorderStyle.FixedSingle;
            lblWhiteClock.Font = new Font("Consolas", 14F, FontStyle.Bold);
            lblWhiteClock.Location = new Point(10, 140);
            lblWhiteClock.Name = "lblWhiteClock";
            lblWhiteClock.Size = new Size(120, 30);
            lblWhiteClock.TabIndex = 7;
            lblWhiteClock.Text = "W: --:--";
            lblWhiteClock.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblBlackClock
            // 
            lblBlackClock.BorderStyle = BorderStyle.FixedSingle;
            lblBlackClock.Font = new Font("Consolas", 14F, FontStyle.Bold);
            lblBlackClock.Location = new Point(140, 140);
            lblBlackClock.Name = "lblBlackClock";
            lblBlackClock.Size = new Size(120, 30);
            lblBlackClock.TabIndex = 8;
            lblBlackClock.Text = "B: --:--";
            lblBlackClock.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btnStartMatch
            // 
            btnStartMatch.FlatStyle = FlatStyle.Flat;
            btnStartMatch.Font = new Font("Courier New", 9F, FontStyle.Bold);
            btnStartMatch.Location = new Point(10, 178);
            btnStartMatch.Name = "btnStartMatch";
            btnStartMatch.Size = new Size(120, 30);
            btnStartMatch.TabIndex = 9;
            btnStartMatch.Text = "Start Match";
            btnStartMatch.Click += BtnStartMatch_Click;
            // 
            // btnStopMatch
            // 
            btnStopMatch.FlatStyle = FlatStyle.Flat;
            btnStopMatch.Font = new Font("Courier New", 9F, FontStyle.Bold);
            btnStopMatch.Location = new Point(140, 178);
            btnStopMatch.Name = "btnStopMatch";
            btnStopMatch.Size = new Size(120, 30);
            btnStopMatch.TabIndex = 10;
            btnStopMatch.Text = "Stop";
            btnStopMatch.Visible = false;
            btnStopMatch.Click += BtnStopMatch_Click;
            // 
            // chkFromPosition
            // 
            chkFromPosition.AutoSize = true;
            chkFromPosition.Font = new Font("Courier New", 9F);
            chkFromPosition.Location = new Point(10, 212);
            chkFromPosition.Name = "chkFromPosition";
            chkFromPosition.Size = new Size(215, 19);
            chkFromPosition.TabIndex = 11;
            chkFromPosition.Text = "Start from current position";
            // 
            // lblAnalysis
            // 
            lblAnalysis.Dock = DockStyle.Top;
            lblAnalysis.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblAnalysis.Location = new Point(5, 5);
            lblAnalysis.Name = "lblAnalysis";
            lblAnalysis.Size = new Size(337, 20);
            lblAnalysis.TabIndex = 0;
            lblAnalysis.Text = "Engine Analysis";
            // 
            // AnalysisBoardForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1068, 646);
            Controls.Add(mainLayout);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new Size(1000, 600);
            Name = "AnalysisBoardForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "chessdroid v3.1.0";
            KeyDown += AnalysisBoardForm_KeyDown;
            mainLayout.ResumeLayout(false);
            leftPanel.ResumeLayout(false);
            leftPanel.PerformLayout();
            middlePanel.ResumeLayout(false);
            pnlPgnButtons.ResumeLayout(false);
            rightPanel.ResumeLayout(false);
            grpEngineMatch.ResumeLayout(false);
            grpEngineMatch.PerformLayout();
            pnlTimeParams.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numDepth).EndInit();
            ((System.ComponentModel.ISupportInitialize)numMoveTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTotalTime).EndInit();
            ((System.ComponentModel.ISupportInitialize)numIncrement).EndInit();
            ResumeLayout(false);
        }

        #endregion

        // Layout panels
        private TableLayoutPanel mainLayout;
        private Panel leftPanel;
        private Panel middlePanel;
        private Panel rightPanel;

        // Left panel - Board and controls
        private EvalBarControl evalBar;
        private ChessBoardControl boardControl;
        private Label lblTurn;
        private Label lblPieces;
        private ComboBox cmbPieces;
        private Button btnSettings;
        private Button btnNewGame;
        private Button btnFlipBoard;
        private Button btnTakeBack;
        private Button btnPrevMove;
        private Button btnNextMove;

        private Label lblFen;
        private TextBox txtFen;
        private Button btnLoadFen;
        private Button btnCopyFen;
        private Label lblStatus;

        // Middle panel - Move list
        private Label lblMoves;
        private ListBox moveListBox;
        private Panel pnlPgnButtons;
        private Button btnClassifyMoves;
        private Button btnExportPgn;
        private Button btnImportPgn;

        // Right panel - Engine Match
        private GroupBox grpEngineMatch;
        private Label lblWhiteEngine;
        private ComboBox cmbWhiteEngine;
        private Label lblBlackEngine;
        private ComboBox cmbBlackEngine;
        private Label lblTimeControl;
        private ComboBox cmbTimeControlType;
        private Panel pnlTimeParams;
        private Label lblDepth;
        private NumericUpDown numDepth;
        private Label lblMoveTime;
        private NumericUpDown numMoveTime;
        private Label lblTotalTime;
        private NumericUpDown numTotalTime;
        private Label lblIncrement;
        private NumericUpDown numIncrement;
        private Label lblWhiteClock;
        private Label lblBlackClock;
        private Button btnStartMatch;
        private Button btnStopMatch;
        private CheckBox chkFromPosition;

        // Right panel - Analysis
        private Label lblAnalysis;
        private RichTextBox analysisOutput;
    }
}
