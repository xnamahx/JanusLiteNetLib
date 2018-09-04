namespace Janus
{
    partial class ServerForm
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
            this.messageGroup = new System.Windows.Forms.GroupBox();
            this.messageText = new System.Windows.Forms.TextBox();
            this.clientGroup = new System.Windows.Forms.GroupBox();
            this.clientGrid = new System.Windows.Forms.DataGridView();
            this.clientIdColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clientRttColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.RecColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.SentColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.timeLineGroup = new System.Windows.Forms.GroupBox();
            this.timeLineGrid = new System.Windows.Forms.DataGridView();
            this.timeLineNameColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Connections = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.messageGroup.SuspendLayout();
            this.clientGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.clientGrid)).BeginInit();
            this.timeLineGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.timeLineGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // messageGroup
            // 
            this.messageGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messageGroup.Controls.Add(this.messageText);
            this.messageGroup.Location = new System.Drawing.Point(13, 13);
            this.messageGroup.Name = "messageGroup";
            this.messageGroup.Size = new System.Drawing.Size(846, 252);
            this.messageGroup.TabIndex = 0;
            this.messageGroup.TabStop = false;
            this.messageGroup.Text = "Messages";
            // 
            // messageText
            // 
            this.messageText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.messageText.Location = new System.Drawing.Point(7, 20);
            this.messageText.Multiline = true;
            this.messageText.Name = "messageText";
            this.messageText.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.messageText.Size = new System.Drawing.Size(833, 226);
            this.messageText.TabIndex = 0;
            // 
            // clientGroup
            // 
            this.clientGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.clientGroup.Controls.Add(this.clientGrid);
            this.clientGroup.Location = new System.Drawing.Point(13, 271);
            this.clientGroup.Name = "clientGroup";
            this.clientGroup.Size = new System.Drawing.Size(402, 243);
            this.clientGroup.TabIndex = 1;
            this.clientGroup.TabStop = false;
            this.clientGroup.Text = "Clients";
            // 
            // clientGrid
            // 
            this.clientGrid.AllowUserToAddRows = false;
            this.clientGrid.AllowUserToDeleteRows = false;
            this.clientGrid.AllowUserToResizeColumns = false;
            this.clientGrid.AllowUserToResizeRows = false;
            this.clientGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.clientGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.clientGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.clientIdColumn,
            this.clientRttColumn,
            this.RecColumn,
            this.SentColumn});
            this.clientGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.clientGrid.Location = new System.Drawing.Point(7, 19);
            this.clientGrid.Name = "clientGrid";
            this.clientGrid.Size = new System.Drawing.Size(412, 218);
            this.clientGrid.TabIndex = 2;
            // 
            // clientIdColumn
            // 
            this.clientIdColumn.HeaderText = "ID";
            this.clientIdColumn.MinimumWidth = 75;
            this.clientIdColumn.Name = "clientIdColumn";
            this.clientIdColumn.ReadOnly = true;
            this.clientIdColumn.Width = 75;
            // 
            // clientRttColumn
            // 
            this.clientRttColumn.HeaderText = "RTT";
            this.clientRttColumn.MinimumWidth = 75;
            this.clientRttColumn.Name = "clientRttColumn";
            this.clientRttColumn.ReadOnly = true;
            this.clientRttColumn.Width = 75;
            // 
            // RecColumn
            // 
            this.RecColumn.HeaderText = "To TLS";
            this.RecColumn.Name = "RecColumn";
            // 
            // SentColumn
            // 
            this.SentColumn.HeaderText = "From TLS";
            this.SentColumn.Name = "SentColumn";
            // 
            // timeLineGroup
            // 
            this.timeLineGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.timeLineGroup.Controls.Add(this.timeLineGrid);
            this.timeLineGroup.Location = new System.Drawing.Point(421, 271);
            this.timeLineGroup.Name = "timeLineGroup";
            this.timeLineGroup.Size = new System.Drawing.Size(438, 243);
            this.timeLineGroup.TabIndex = 3;
            this.timeLineGroup.TabStop = false;
            this.timeLineGroup.Text = "Timelines";
            // 
            // timeLineGrid
            // 
            this.timeLineGrid.AllowUserToAddRows = false;
            this.timeLineGrid.AllowUserToDeleteRows = false;
            this.timeLineGrid.AllowUserToResizeColumns = false;
            this.timeLineGrid.AllowUserToResizeRows = false;
            this.timeLineGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.timeLineGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.timeLineGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.timeLineNameColumn,
            this.Connections});
            this.timeLineGrid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
            this.timeLineGrid.Location = new System.Drawing.Point(7, 19);
            this.timeLineGrid.Name = "timeLineGrid";
            this.timeLineGrid.Size = new System.Drawing.Size(425, 218);
            this.timeLineGrid.TabIndex = 2;
            this.timeLineGrid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnTimelineCellClick);
            // 
            // timeLineNameColumn
            // 
            this.timeLineNameColumn.HeaderText = "Name";
            this.timeLineNameColumn.Name = "timeLineNameColumn";
            this.timeLineNameColumn.ReadOnly = true;
            this.timeLineNameColumn.Width = 200;
            // 
            // Connections
            // 
            this.Connections.FillWeight = 105F;
            this.Connections.HeaderText = "Connections";
            this.Connections.Name = "Connections";
            this.Connections.Width = 150;
            // 
            // ServerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(867, 526);
            this.Controls.Add(this.timeLineGroup);
            this.Controls.Add(this.clientGroup);
            this.Controls.Add(this.messageGroup);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "ServerForm";
            this.Text = "TimelineServer";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.OnFormClosed);
            this.Load += new System.EventHandler(this.ServerForm_Load);
            this.messageGroup.ResumeLayout(false);
            this.messageGroup.PerformLayout();
            this.clientGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.clientGrid)).EndInit();
            this.timeLineGroup.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.timeLineGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox messageGroup;
        private System.Windows.Forms.TextBox messageText;
        private System.Windows.Forms.GroupBox clientGroup;
        private System.Windows.Forms.DataGridView clientGrid;
        private System.Windows.Forms.GroupBox timeLineGroup;
        private System.Windows.Forms.DataGridView timeLineGrid;
        private System.Windows.Forms.DataGridViewTextBoxColumn clientIdColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn clientRttColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn RecColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn SentColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn timeLineNameColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn Connections;
    }
}