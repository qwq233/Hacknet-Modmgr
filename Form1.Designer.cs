namespace ModManagerInstall
{
    partial class Form1
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.PathOfHacknet = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.Install = new System.Windows.Forms.Button();
            this.Unstall = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("宋体", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(70, 14);
            this.label1.TabIndex = 0;
            this.label1.Text = "Hacknet：";
            // 
            // PathOfHacknet
            // 
            this.PathOfHacknet.AllowDrop = true;
            this.PathOfHacknet.Location = new System.Drawing.Point(77, 6);
            this.PathOfHacknet.Name = "PathOfHacknet";
            this.PathOfHacknet.Size = new System.Drawing.Size(287, 21);
            this.PathOfHacknet.TabIndex = 1;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(370, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(36, 21);
            this.button1.TabIndex = 2;
            this.button1.Text = "...";
            this.button1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.button1.UseVisualStyleBackColor = true;
            // 
            // Install
            // 
            this.Install.Location = new System.Drawing.Point(12, 33);
            this.Install.Name = "Install";
            this.Install.Size = new System.Drawing.Size(188, 26);
            this.Install.TabIndex = 3;
            this.Install.Text = "安装";
            this.Install.UseVisualStyleBackColor = true;
            this.Install.Click += new System.EventHandler(this.Install_Click);
            // 
            // Unstall
            // 
            this.Unstall.Location = new System.Drawing.Point(206, 33);
            this.Unstall.Name = "Unstall";
            this.Unstall.Size = new System.Drawing.Size(200, 26);
            this.Unstall.TabIndex = 4;
            this.Unstall.Text = "卸载";
            this.Unstall.UseVisualStyleBackColor = true;
            this.Unstall.Click += new System.EventHandler(this.Unstall_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(418, 68);
            this.Controls.Add(this.Unstall);
            this.Controls.Add(this.Install);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.PathOfHacknet);
            this.Controls.Add(this.label1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "Form1";
            this.Text = "ModManger Installer";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox PathOfHacknet;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button Unstall;
        private System.Windows.Forms.Button Install;
    }
}

