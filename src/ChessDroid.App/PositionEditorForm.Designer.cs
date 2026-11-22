namespace ChessDroid
{
    partial class PositionEditorForm
    {
#pragma warning disable CS8618
        private System.ComponentModel.IContainer components = null!;
#pragma warning restore CS8618

        private void InitializeComponent()
        {
            pnlBoard = new Panel();
            lblPalette = new Label();
            lblSideToMove = new Label();
            rdoWhite = new RadioButton();
            rdoBlack = new RadioButton();
            lblCastling = new Label();
            chkCastleWK = new CheckBox();
            chkCastleWQ = new CheckBox();
            chkCastleBK = new CheckBox();
            chkCastleBQ = new CheckBox();
            btnClear = new Button();
            btnStartPos = new Button();
            lblStatus = new Label();
            btnApply = new Button();
            btnCancel = new Button();
            SuspendLayout();
            // 
            // pnlBoard
            // 
            pnlBoard.BorderStyle = BorderStyle.FixedSingle;
            pnlBoard.Location = new Point(12, 11);
            pnlBoard.Name = "pnlBoard";
            pnlBoard.Size = new Size(480, 480);
            pnlBoard.TabIndex = 0;
            pnlBoard.Paint += PnlBoard_Paint;
            pnlBoard.MouseClick += PnlBoard_MouseClick;
            // 
            // lblPalette
            // 
            lblPalette.Font = new Font("Consolas", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblPalette.Location = new Point(504, 11);
            lblPalette.Name = "lblPalette";
            lblPalette.Size = new Size(195, 17);
            lblPalette.TabIndex = 1;
            lblPalette.Text = "Palette";
            // 
            // lblSideToMove
            // 
            lblSideToMove.Location = new Point(522, 339);
            lblSideToMove.Name = "lblSideToMove";
            lblSideToMove.Size = new Size(111, 17);
            lblSideToMove.TabIndex = 2;
            lblSideToMove.Text = "Side to move:";
            // 
            // rdoWhite
            // 
            rdoWhite.Checked = true;
            rdoWhite.Location = new Point(504, 359);
            rdoWhite.Name = "rdoWhite";
            rdoWhite.Size = new Size(70, 19);
            rdoWhite.TabIndex = 3;
            rdoWhite.TabStop = true;
            rdoWhite.Text = "White";
            rdoWhite.CheckedChanged += RdoWhite_CheckedChanged;
            // 
            // rdoBlack
            // 
            rdoBlack.Location = new Point(582, 359);
            rdoBlack.Name = "rdoBlack";
            rdoBlack.Size = new Size(70, 19);
            rdoBlack.TabIndex = 4;
            rdoBlack.Text = "Black";
            rdoBlack.CheckedChanged += RdoBlack_CheckedChanged;
            // 
            // lblCastling
            // 
            lblCastling.Location = new Point(515, 383);
            lblCastling.Name = "lblCastling";
            lblCastling.Size = new Size(195, 17);
            lblCastling.TabIndex = 5;
            lblCastling.Text = "Castling rights:";
            // 
            // chkCastleWK
            // 
            chkCastleWK.Checked = true;
            chkCastleWK.CheckState = CheckState.Checked;
            chkCastleWK.Location = new Point(532, 403);
            chkCastleWK.Name = "chkCastleWK";
            chkCastleWK.Size = new Size(44, 19);
            chkCastleWK.TabIndex = 6;
            chkCastleWK.Text = "WK";
            chkCastleWK.CheckedChanged += ChkCastleWK_CheckedChanged;
            // 
            // chkCastleWQ
            // 
            chkCastleWQ.Checked = true;
            chkCastleWQ.CheckState = CheckState.Checked;
            chkCastleWQ.Location = new Point(579, 403);
            chkCastleWQ.Name = "chkCastleWQ";
            chkCastleWQ.Size = new Size(44, 19);
            chkCastleWQ.TabIndex = 7;
            chkCastleWQ.Text = "WQ";
            chkCastleWQ.CheckedChanged += ChkCastleWQ_CheckedChanged;
            // 
            // chkCastleBK
            // 
            chkCastleBK.Checked = true;
            chkCastleBK.CheckState = CheckState.Checked;
            chkCastleBK.Location = new Point(532, 426);
            chkCastleBK.Name = "chkCastleBK";
            chkCastleBK.Size = new Size(44, 19);
            chkCastleBK.TabIndex = 8;
            chkCastleBK.Text = "BK";
            chkCastleBK.CheckedChanged += ChkCastleBK_CheckedChanged;
            // 
            // chkCastleBQ
            // 
            chkCastleBQ.Checked = true;
            chkCastleBQ.CheckState = CheckState.Checked;
            chkCastleBQ.Location = new Point(579, 426);
            chkCastleBQ.Name = "chkCastleBQ";
            chkCastleBQ.Size = new Size(50, 19);
            chkCastleBQ.TabIndex = 9;
            chkCastleBQ.Text = "BQ";
            chkCastleBQ.CheckedChanged += ChkCastleBQ_CheckedChanged;
            // 
            // btnClear
            // 
            btnClear.FlatStyle = FlatStyle.Flat;
            btnClear.Location = new Point(508, 454);
            btnClear.Name = "btnClear";
            btnClear.Size = new Size(132, 26);
            btnClear.TabIndex = 10;
            btnClear.Text = "Clear Board";
            btnClear.Click += BtnClear_Click;
            // 
            // btnStartPos
            // 
            btnStartPos.FlatStyle = FlatStyle.Flat;
            btnStartPos.Location = new Point(508, 486);
            btnStartPos.Name = "btnStartPos";
            btnStartPos.Size = new Size(132, 26);
            btnStartPos.TabIndex = 11;
            btnStartPos.Text = "Starting Pos";
            btnStartPos.Click += BtnStartPos_Click;
            // 
            // lblStatus
            // 
            lblStatus.Location = new Point(12, 504);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(480, 19);
            lblStatus.TabIndex = 12;
            lblStatus.Text = "Select a piece from the palette, then click a square";
            // 
            // btnApply
            // 
            btnApply.FlatStyle = FlatStyle.Flat;
            btnApply.Location = new Point(508, 522);
            btnApply.Name = "btnApply";
            btnApply.Size = new Size(132, 26);
            btnApply.TabIndex = 13;
            btnApply.Text = "Apply";
            btnApply.Click += BtnApply_Click;
            // 
            // btnCancel
            // 
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.Location = new Point(508, 553);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(132, 26);
            btnCancel.TabIndex = 14;
            btnCancel.Text = "Cancel";
            btnCancel.Click += BtnCancel_Click;
            // 
            // PositionEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 14F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(648, 592);
            Controls.Add(pnlBoard);
            Controls.Add(lblPalette);
            Controls.Add(lblSideToMove);
            Controls.Add(rdoWhite);
            Controls.Add(rdoBlack);
            Controls.Add(lblCastling);
            Controls.Add(chkCastleWK);
            Controls.Add(chkCastleWQ);
            Controls.Add(chkCastleBK);
            Controls.Add(chkCastleBQ);
            Controls.Add(btnClear);
            Controls.Add(btnStartPos);
            Controls.Add(lblStatus);
            Controls.Add(btnApply);
            Controls.Add(btnCancel);
            Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PositionEditorForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Edit Position";
            ResumeLayout(false);
        }

        // ── Control fields ─────────────────────────────────────────────
        private Panel       pnlBoard;
        private Label       lblPalette;
        private Label       lblSideToMove;
        private RadioButton rdoWhite;
        private RadioButton rdoBlack;
        private Label       lblCastling;
        private CheckBox    chkCastleWK, chkCastleWQ, chkCastleBK, chkCastleBQ;
        private Button      btnClear, btnStartPos;
        private Label       lblStatus;
        private Button      btnApply, btnCancel;
    }
}
