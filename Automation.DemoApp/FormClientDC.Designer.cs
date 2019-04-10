namespace Automation.DemoApp
{
	partial class FormClientDC
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
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btnCapture = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.txtHandle = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.SuspendLayout();
			// 
			// pictureBox1
			// 
			this.pictureBox1.BackColor = System.Drawing.SystemColors.ButtonShadow;
			this.pictureBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pictureBox1.Location = new System.Drawing.Point(12, 12);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(574, 310);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			// 
			// btnCapture
			// 
			this.btnCapture.Location = new System.Drawing.Point(356, 337);
			this.btnCapture.Name = "btnCapture";
			this.btnCapture.Size = new System.Drawing.Size(75, 23);
			this.btnCapture.TabIndex = 1;
			this.btnCapture.Text = "Capture";
			this.btnCapture.UseVisualStyleBackColor = true;
			this.btnCapture.Click += new System.EventHandler(this.btnCapture_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(112, 342);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(84, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "Window handle:";
			// 
			// txtHandle
			// 
			this.txtHandle.Location = new System.Drawing.Point(202, 339);
			this.txtHandle.Name = "txtHandle";
			this.txtHandle.Size = new System.Drawing.Size(148, 20);
			this.txtHandle.TabIndex = 3;
			// 
			// FormClientDC
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(598, 369);
			this.Controls.Add(this.txtHandle);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnCapture);
			this.Controls.Add(this.pictureBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.Name = "FormClientDC";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "FormClientDC";
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.Button btnCapture;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox txtHandle;
	}
}