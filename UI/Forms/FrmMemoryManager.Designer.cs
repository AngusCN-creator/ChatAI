namespace ChatAI.UI.Forms
{
    partial class FrmMemoryManager
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
            splitContainer1 = new SplitContainer();
            lbxRoleList = new ListBox();
            splitContainer2 = new SplitContainer();
            dgvMemories = new DataGridView();
            splitContainer3 = new SplitContainer();
            btnDeleteMemory = new Button();
            btnSaveMemory = new Button();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvMemories).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.FixedPanel = FixedPanel.Panel1;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(lbxRoleList);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(703, 682);
            splitContainer1.SplitterDistance = 175;
            splitContainer1.TabIndex = 0;
            // 
            // lbxRoleList
            // 
            lbxRoleList.Dock = DockStyle.Fill;
            lbxRoleList.FormattingEnabled = true;
            lbxRoleList.Location = new Point(0, 0);
            lbxRoleList.Name = "lbxRoleList";
            lbxRoleList.Size = new Size(175, 682);
            lbxRoleList.TabIndex = 0;
            lbxRoleList.SelectedIndexChanged += lbxRoleList_SelectedIndexChanged;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(dgvMemories);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(524, 682);
            splitContainer2.SplitterDistance = 625;
            splitContainer2.TabIndex = 0;
            // 
            // dgvMemories
            // 
            dgvMemories.Dock = DockStyle.Fill;
            dgvMemories.Location = new Point(0, 0);
            dgvMemories.Name = "dgvMemories";
            dgvMemories.Size = new Size(524, 625);
            dgvMemories.TabIndex = 0;
            dgvMemories.CellDoubleClick += dgvMemories_CellDoubleClick;
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.Controls.Add(btnDeleteMemory);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(btnSaveMemory);
            splitContainer3.Size = new Size(524, 53);
            splitContainer3.SplitterDistance = 251;
            splitContainer3.TabIndex = 0;
            // 
            // btnDeleteMemory
            // 
            btnDeleteMemory.BackColor = Color.SandyBrown;
            btnDeleteMemory.Dock = DockStyle.Fill;
            btnDeleteMemory.FlatStyle = FlatStyle.Popup;
            btnDeleteMemory.Location = new Point(0, 0);
            btnDeleteMemory.Name = "btnDeleteMemory";
            btnDeleteMemory.Size = new Size(251, 53);
            btnDeleteMemory.TabIndex = 0;
            btnDeleteMemory.Text = "删除记忆";
            btnDeleteMemory.UseVisualStyleBackColor = false;
            btnDeleteMemory.Click += btnDeleteMemory_Click;
            // 
            // btnSaveMemory
            // 
            btnSaveMemory.BackColor = Color.DeepSkyBlue;
            btnSaveMemory.Dock = DockStyle.Fill;
            btnSaveMemory.FlatStyle = FlatStyle.Popup;
            btnSaveMemory.Location = new Point(0, 0);
            btnSaveMemory.Name = "btnSaveMemory";
            btnSaveMemory.Size = new Size(269, 53);
            btnSaveMemory.TabIndex = 0;
            btnSaveMemory.Text = "保存记忆";
            btnSaveMemory.UseVisualStyleBackColor = false;
            btnSaveMemory.Click += btnSaveMemory_Click;
            // 
            // FrmMemoryManager
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(703, 682);
            Controls.Add(splitContainer1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FrmMemoryManager";
            StartPosition = FormStartPosition.CenterParent;
            Text = "记忆管理窗口";
            Load += FrmMemoryManager_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvMemories).EndInit();
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private ListBox lbxRoleList;
        private SplitContainer splitContainer2;
        private DataGridView dgvMemories;
        private SplitContainer splitContainer3;
        private Button btnDeleteMemory;
        private Button btnSaveMemory;
    }
}
