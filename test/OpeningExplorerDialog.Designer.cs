namespace ChessDroid
{
    partial class OpeningExplorerDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            lblSearch = new Label();
            txtSearch = new TextBox();
            grid = new DataGridView();
            colEco = new DataGridViewTextBoxColumn();
            colName = new DataGridViewTextBoxColumn();
            colEval = new DataGridViewTextBoxColumn();
            lblMoves = new Label();
            btnLoad = new Button();
            btnClose = new Button();
            ((System.ComponentModel.ISupportInitialize)grid).BeginInit();
            SuspendLayout();
            // 
            // lblSearch
            // 
            lblSearch.Font = new Font("Courier New", 9F);
            lblSearch.Location = new Point(13, 14);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(52, 20);
            lblSearch.TabIndex = 0;
            lblSearch.Text = "Search:";
            // 
            // txtSearch
            // 
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Font = new Font("Courier New", 9F);
            txtSearch.Location = new Point(67, 10);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(724, 21);
            txtSearch.TabIndex = 1;
            // 
            // grid
            // 
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            grid.BackgroundColor = SystemColors.Window;
            grid.BorderStyle = BorderStyle.Fixed3D;
            grid.ColumnHeadersHeight = 24;
            grid.Columns.AddRange(new DataGridViewColumn[] { colEco, colName, colEval });
            grid.Font = new Font("Courier New", 9F);
            grid.Location = new Point(13, 40);
            grid.MultiSelect = false;
            grid.Name = "grid";
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.RowTemplate.Height = 22;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.Size = new Size(778, 420);
            grid.TabIndex = 1;
            // 
            // colEco
            // 
            colEco.HeaderText = "ECO";
            colEco.Name = "colEco";
            colEco.ReadOnly = true;
            colEco.Resizable = DataGridViewTriState.False;
            colEco.SortMode = DataGridViewColumnSortMode.NotSortable;
            colEco.Width = 60;
            // 
            // colName
            // 
            colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colName.HeaderText = "Opening Name";
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.SortMode = DataGridViewColumnSortMode.NotSortable;
            colName.Width = 560;
            // 
            // colEval
            // 
            colEval.HeaderText = "Eval";
            colEval.Name = "colEval";
            colEval.ReadOnly = true;
            colEval.SortMode = DataGridViewColumnSortMode.NotSortable;
            colEval.Width = 68;
            // 
            // lblMoves
            // 
            lblMoves.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblMoves.AutoEllipsis = true;
            lblMoves.Font = new Font("Courier New", 8.25F);
            lblMoves.Location = new Point(13, 467);
            lblMoves.Name = "lblMoves";
            lblMoves.Size = new Size(778, 20);
            lblMoves.TabIndex = 2;
            // 
            // btnLoad
            // 
            btnLoad.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnLoad.Enabled = false;
            btnLoad.FlatStyle = FlatStyle.Flat;
            btnLoad.Font = new Font("Courier New", 9F);
            btnLoad.Location = new Point(583, 492);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(120, 28);
            btnLoad.TabIndex = 2;
            btnLoad.Text = "Load Opening";
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnClose.DialogResult = DialogResult.Cancel;
            btnClose.FlatStyle = FlatStyle.Flat;
            btnClose.Font = new Font("Courier New", 9F);
            btnClose.Location = new Point(711, 492);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(80, 28);
            btnClose.TabIndex = 3;
            btnClose.Text = "Close";
            // 
            // OpeningExplorerDialog
            // 
            AcceptButton = btnLoad;
            CancelButton = btnClose;
            ClientSize = new Size(804, 531);
            Controls.Add(lblSearch);
            Controls.Add(txtSearch);
            Controls.Add(grid);
            Controls.Add(lblMoves);
            Controls.Add(btnLoad);
            Controls.Add(btnClose);
            Font = new Font("Courier New", 9F);
            MaximizeBox = false;
            MinimumSize = new Size(560, 440);
            Name = "OpeningExplorerDialog";
            SizeGripStyle = SizeGripStyle.Hide;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Opening Explorer";
            ((System.ComponentModel.ISupportInitialize)grid).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private System.Windows.Forms.Label                       lblSearch;
        private System.Windows.Forms.TextBox                     txtSearch;
        private System.Windows.Forms.DataGridView                grid;
        private System.Windows.Forms.DataGridViewTextBoxColumn   colEco;
        private System.Windows.Forms.DataGridViewTextBoxColumn   colName;
        private System.Windows.Forms.DataGridViewTextBoxColumn   colEval;
        private System.Windows.Forms.Label                       lblMoves;
        private System.Windows.Forms.Button                      btnLoad;
        private System.Windows.Forms.Button                      btnClose;
    }
}
