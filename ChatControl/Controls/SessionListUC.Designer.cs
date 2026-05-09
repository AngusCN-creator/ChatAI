﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using ChatControl.Models;
namespace ChatControl.Controls
{
    partial class SessionListUC
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

        #region 组件设计器生成的代码
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            lstSession = new ListBox();
            btnCreateSession = new Button();
            contextMenuStrip1 = new ContextMenuStrip(components);
            tsmiDeleteSession = new ToolStripMenuItem();
            tsmiEditSession = new ToolStripMenuItem();
            tsmiViewMemory = new ToolStripMenuItem();
            contextMenuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // lstSession
            // 
            lstSession.BackColor = Color.Gainsboro;
            lstSession.BorderStyle = BorderStyle.None;
            lstSession.Dock = DockStyle.Fill;
            lstSession.DrawMode = DrawMode.OwnerDrawFixed;
            lstSession.Font = new Font("微软雅黑", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            lstSession.ForeColor = Color.FromArgb(64, 64, 64);
            lstSession.ItemHeight = 60;
            lstSession.Location = new Point(0, 0);
            lstSession.Name = "lstSession";
            lstSession.Size = new Size(204, 620);
            lstSession.TabIndex = 0;
            lstSession.MouseDown += lstSession_MouseDown;
            // 
            // btnCreateSession
            // 
            btnCreateSession.BackColor = Color.Silver;
            btnCreateSession.Dock = DockStyle.Bottom;
            btnCreateSession.FlatAppearance.BorderSize = 0;
            btnCreateSession.FlatStyle = FlatStyle.Flat;
            btnCreateSession.Font = new Font("微软雅黑", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 134);
            btnCreateSession.ForeColor = Color.Black;
            btnCreateSession.Location = new Point(0, 620);
            btnCreateSession.Name = "btnCreateSession";
            btnCreateSession.Size = new Size(204, 53);
            btnCreateSession.TabIndex = 1;
            btnCreateSession.Text = "创建新会话 💬";
            btnCreateSession.UseVisualStyleBackColor = false;
            btnCreateSession.Click += btnCreateSession_Click;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { tsmiEditSession, tsmiViewMemory, tsmiDeleteSession });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(125, 70);
            // 
            // tsmiEditSession
            // 
            tsmiEditSession.Name = "tsmiEditSession";
            tsmiEditSession.Size = new Size(124, 22);
            tsmiEditSession.Text = "编辑会话";
            tsmiEditSession.Click += tsmiEditSession_Click;
            // 
            // tsmiViewMemory
            // 
            tsmiViewMemory.Name = "tsmiViewMemory";
            tsmiViewMemory.Size = new Size(124, 22);
            tsmiViewMemory.Text = "查看记忆";
            tsmiViewMemory.Click += tsmiViewMemory_Click;
            // 
            // tsmiDeleteSession
            // 
            tsmiDeleteSession.Name = "tsmiDeleteSession";
            tsmiDeleteSession.Size = new Size(124, 22);
            tsmiDeleteSession.Text = "删除会话";
            tsmiDeleteSession.Click += tsmiDeleteSession_Click;
            // 
            // SessionListUC
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Gainsboro;
            Controls.Add(lstSession);
            Controls.Add(btnCreateSession);
            Name = "SessionListUC";
            Size = new Size(204, 673);
            contextMenuStrip1.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion

        private System.Windows.Forms.ListBox lstSession;
        private System.Windows.Forms.Button btnCreateSession;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tsmiDeleteSession;
        private System.Windows.Forms.ToolStripMenuItem tsmiEditSession;
        private System.Windows.Forms.ToolStripMenuItem tsmiViewMemory;
    }
}