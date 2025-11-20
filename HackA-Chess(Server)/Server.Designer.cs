namespace HackA_Chess_Server_
{
    partial class Server
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            rtb_servernotify = new RichTextBox();
            label3 = new Label();
            rtb_clientsconnection = new RichTextBox();
            label1 = new Label();
            SuspendLayout();
            // 
            // rtb_servernotify
            // 
            rtb_servernotify.Location = new Point(44, 95);
            rtb_servernotify.Name = "rtb_servernotify";
            rtb_servernotify.ReadOnly = true;
            rtb_servernotify.Size = new Size(529, 420);
            rtb_servernotify.TabIndex = 4;
            rtb_servernotify.Text = "";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.Location = new Point(388, 35);
            label3.Name = "label3";
            label3.Size = new Size(84, 28);
            label3.TabIndex = 5;
            label3.Text = "SERVER";
            label3.Click += label3_Click;
            // 
            // rtb_clientsconnection
            // 
            rtb_clientsconnection.Location = new Point(594, 95);
            rtb_clientsconnection.Name = "rtb_clientsconnection";
            rtb_clientsconnection.ReadOnly = true;
            rtb_clientsconnection.Size = new Size(248, 430);
            rtb_clientsconnection.TabIndex = 0;
            rtb_clientsconnection.Text = "";
            rtb_clientsconnection.TextChanged += rtb_clientsconnection_TextChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(594, 72);
            label1.Name = "label1";
            label1.Size = new Size(165, 20);
            label1.TabIndex = 1;
            label1.Text = "Các Client đang kết nối:";
            label1.Click += label1_Click;
            // 
            // Server
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(854, 567);
            Controls.Add(label3);
            Controls.Add(rtb_servernotify);
            Controls.Add(label1);
            Controls.Add(rtb_clientsconnection);
            Name = "Server";
            Text = "TCP SERVER";
            Load += TCPServer_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private RichTextBox rtb_servernotify;
        private Label label3;
        private RichTextBox rtb_clientsconnection;
        private Label label1;
    }
}
