namespace ChatControl.Controls
{
    partial class ChatMainControl
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panelChat = new Panel();
            btnSend = new Button();
            btnInsertBracket = new Button();
            txtInput = new TextBox();
            splitContainer1 = new SplitContainer();
            lbl_CurrentChatName = new Label();
            splitContainer2 = new SplitContainer();
            btnRetryReply = new Button();
            toolTip_QuickButtons = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            SuspendLayout();
            // 
            // panelChat
            // 
            panelChat.BackColor = Color.Gainsboro;
            panelChat.Dock = DockStyle.Fill;
            panelChat.Location = new Point(0, 0);
            panelChat.Name = "panelChat";
            panelChat.Size = new Size(567, 451);
            panelChat.TabIndex = 0;
            // 
            // btnSend
            // 
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.BackColor = Color.SandyBrown;
            btnSend.FlatStyle = FlatStyle.Popup;
            btnSend.Location = new Point(489, 84);
            btnSend.Name = "btnSend";
            btnSend.Size = new Size(75, 23);
            btnSend.TabIndex = 1;
            btnSend.Text = "发送";
            btnSend.UseVisualStyleBackColor = false;
            // 
            // btnInsertBracket
            // 
            btnInsertBracket.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnInsertBracket.BackColor = Color.Gainsboro;
            btnInsertBracket.FlatAppearance.BorderSize = 0;
            btnInsertBracket.FlatAppearance.MouseOverBackColor = Color.DarkOrange;
            btnInsertBracket.FlatStyle = FlatStyle.Flat;
            btnInsertBracket.Location = new Point(5, 81);
            btnInsertBracket.Name = "btnInsertBracket";
            btnInsertBracket.Size = new Size(40, 23);
            btnInsertBracket.TabIndex = 2;
            btnInsertBracket.Text = "（）";
            toolTip_QuickButtons.SetToolTip(btnInsertBracket, "快捷括号输入");
            btnInsertBracket.UseVisualStyleBackColor = false;
            // 
            // txtInput
            // 
            txtInput.BackColor = Color.Gainsboro;
            txtInput.Dock = DockStyle.Fill;
            txtInput.Location = new Point(0, 0);
            txtInput.Multiline = true;
            txtInput.Name = "txtInput";
            txtInput.Size = new Size(567, 110);
            txtInput.TabIndex = 0;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.IsSplitterFixed = true;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.BackColor = Color.Gainsboro;
            splitContainer1.Panel1.Controls.Add(lbl_CurrentChatName);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.BackColor = Color.White;
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(567, 594);
            splitContainer1.SplitterDistance = 28;
            splitContainer1.SplitterWidth = 1;
            splitContainer1.TabIndex = 2;
            // 
            // lbl_CurrentChatName
            // 
            lbl_CurrentChatName.Anchor = AnchorStyles.Top | AnchorStyles.Bottom;
            lbl_CurrentChatName.AutoSize = true;
            lbl_CurrentChatName.Cursor = Cursors.Hand;
            lbl_CurrentChatName.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lbl_CurrentChatName.ForeColor = SystemColors.ActiveCaptionText;
            lbl_CurrentChatName.Location = new Point(0, 0);
            lbl_CurrentChatName.Name = "lbl_CurrentChatName";
            lbl_CurrentChatName.Size = new Size(0, 21);
            lbl_CurrentChatName.TabIndex = 3;
            lbl_CurrentChatName.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // splitContainer2
            // 
            splitContainer2.BackColor = Color.Gainsboro;
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.FixedPanel = FixedPanel.Panel2;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            splitContainer2.Orientation = Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(panelChat);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(btnRetryReply);
            splitContainer2.Panel2.Controls.Add(btnSend);
            splitContainer2.Panel2.Controls.Add(btnInsertBracket);
            splitContainer2.Panel2.Controls.Add(txtInput);
            splitContainer2.Size = new Size(567, 565);
            splitContainer2.SplitterDistance = 451;
            splitContainer2.TabIndex = 0;
            // 
            // btnRetryReply
            // 
            btnRetryReply.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRetryReply.BackColor = Color.Gainsboro;
            btnRetryReply.FlatAppearance.BorderSize = 0;
            btnRetryReply.FlatAppearance.MouseOverBackColor = Color.DarkOrange;
            btnRetryReply.FlatStyle = FlatStyle.Flat;
            btnRetryReply.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            btnRetryReply.Location = new Point(51, 81);
            btnRetryReply.Name = "btnRetryReply";
            btnRetryReply.Size = new Size(40, 23);
            btnRetryReply.TabIndex = 3;
            btnRetryReply.Text = "←";
            toolTip_QuickButtons.SetToolTip(btnRetryReply, "重新回复");
            btnRetryReply.UseVisualStyleBackColor = false;
            // 
            // ChatMainControl
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(splitContainer1);
            Name = "ChatMainControl";
            Size = new Size(567, 594);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion
        private TextBox txtInput;
        private Button btnSend;
        private Button btnInsertBracket;
        private Panel panelChat;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        public Label lbl_CurrentChatName;
        private Button btnRetryReply;
        private ToolTip toolTip_QuickButtons;
    }
}
