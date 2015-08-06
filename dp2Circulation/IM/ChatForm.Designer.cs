﻿namespace dp2Circulation
{
    partial class ChatForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChatForm));
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.panel_input = new System.Windows.Forms.Panel();
            this.button_send = new System.Windows.Forms.Button();
            this.textBox_input = new System.Windows.Forms.TextBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton_searchBiblio = new System.Windows.Forms.ToolStripButton();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel_input.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel1.Controls.Add(this.webBrowser1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel_input, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.toolStrip1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(305, 264);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // webBrowser1
            // 
            this.webBrowser1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.webBrowser1.Location = new System.Drawing.Point(3, 3);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(305, 153);
            this.webBrowser1.TabIndex = 0;
            // 
            // panel_input
            // 
            this.panel_input.Controls.Add(this.button_send);
            this.panel_input.Controls.Add(this.textBox_input);
            this.panel_input.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_input.Location = new System.Drawing.Point(3, 162);
            this.panel_input.Name = "panel_input";
            this.panel_input.Size = new System.Drawing.Size(305, 74);
            this.panel_input.TabIndex = 1;
            // 
            // button_send
            // 
            this.button_send.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_send.Location = new System.Drawing.Point(227, 4);
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(78, 23);
            this.button_send.TabIndex = 1;
            this.button_send.Text = "发送";
            this.button_send.UseVisualStyleBackColor = true;
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            // 
            // textBox_input
            // 
            this.textBox_input.AcceptsReturn = true;
            this.textBox_input.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox_input.Location = new System.Drawing.Point(4, 4);
            this.textBox_input.MinimumSize = new System.Drawing.Size(50, 4);
            this.textBox_input.Multiline = true;
            this.textBox_input.Name = "textBox_input";
            this.textBox_input.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox_input.Size = new System.Drawing.Size(217, 70);
            this.textBox_input.TabIndex = 0;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton_searchBiblio});
            this.toolStrip1.Location = new System.Drawing.Point(0, 239);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(311, 25);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton_searchBiblio
            // 
            this.toolStripButton_searchBiblio.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.toolStripButton_searchBiblio.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton_searchBiblio.Image")));
            this.toolStripButton_searchBiblio.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton_searchBiblio.Name = "toolStripButton_searchBiblio";
            this.toolStripButton_searchBiblio.Size = new System.Drawing.Size(60, 22);
            this.toolStripButton_searchBiblio.Text = "书目检索";
            this.toolStripButton_searchBiblio.Click += new System.EventHandler(this.toolStripButton_searchBiblio_Click);
            // 
            // ChatForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(305, 264);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ChatForm";
            this.Text = "聊天";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.IMForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.IMForm_FormClosed);
            this.Load += new System.EventHandler(this.IMForm_Load);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel_input.ResumeLayout(false);
            this.panel_input.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.Panel panel_input;
        private System.Windows.Forms.Button button_send;
        private System.Windows.Forms.TextBox textBox_input;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButton_searchBiblio;
    }
}