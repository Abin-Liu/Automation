namespace Automation.DemoApp
{
	partial class Form1
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
			this.btnStart = new System.Windows.Forms.Button();
			this.btnStop = new System.Windows.Forms.Button();
			this.btnClientDC = new System.Windows.Forms.Button();
			this.btnCursorFetch = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(55, 72);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(75, 23);
			this.btnStart.TabIndex = 0;
			this.btnStart.Text = "Start";
			this.btnStart.UseVisualStyleBackColor = true;
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// btnStop
			// 
			this.btnStop.Enabled = false;
			this.btnStop.Location = new System.Drawing.Point(136, 72);
			this.btnStop.Name = "btnStop";
			this.btnStop.Size = new System.Drawing.Size(75, 23);
			this.btnStop.TabIndex = 0;
			this.btnStop.Text = "Stop";
			this.btnStop.UseVisualStyleBackColor = true;
			this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
			// 
			// btnClientDC
			// 
			this.btnClientDC.Location = new System.Drawing.Point(217, 72);
			this.btnClientDC.Name = "btnClientDC";
			this.btnClientDC.Size = new System.Drawing.Size(75, 23);
			this.btnClientDC.TabIndex = 0;
			this.btnClientDC.Text = "ClientDC";
			this.btnClientDC.UseVisualStyleBackColor = true;
			this.btnClientDC.Click += new System.EventHandler(this.btnClientDC_Click);
			// 
			// btnCursorFetch
			// 
			this.btnCursorFetch.Location = new System.Drawing.Point(298, 72);
			this.btnCursorFetch.Name = "btnCursorFetch";
			this.btnCursorFetch.Size = new System.Drawing.Size(75, 23);
			this.btnCursorFetch.TabIndex = 1;
			this.btnCursorFetch.Text = "CursorFetch";
			this.btnCursorFetch.UseVisualStyleBackColor = true;
			this.btnCursorFetch.Click += new System.EventHandler(this.btnCursorFetch_Click);
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(497, 149);
			this.Controls.Add(this.btnCursorFetch);
			this.Controls.Add(this.btnClientDC);
			this.Controls.Add(this.btnStop);
			this.Controls.Add(this.btnStart);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.Name = "Form1";
			this.Text = "Automation Demo";
			this.Load += new System.EventHandler(this.Form1_Load);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.Button btnStop;
		private System.Windows.Forms.Button btnClientDC;
		private System.Windows.Forms.Button btnCursorFetch;
	}
}

