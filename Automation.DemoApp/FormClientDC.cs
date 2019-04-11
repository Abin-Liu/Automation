using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Automation;

namespace Automation.DemoApp
{
	public partial class FormClientDC : Form
	{
		public FormClientDC()
		{
			InitializeComponent();
		}

		private void btnCapture_Click(object sender, EventArgs e)
		{
			string text = txtHandle.Text.Trim();
			try
			{
				IntPtr hwnd = (IntPtr)Convert.ToInt32(text, 16);
				Rectangle rect = Win32API.Window.GetWindowRect(hwnd);
				MemDC dc = new MemDC();
				dc.Capture(rect.Left, rect.Top, 100, 100);
				int pixel = dc.GetPixel(80, 80);
				pictureBox1.Image = dc.Bitmap;
			}
			catch
			{
				MessageBox.Show("Window handle is required.");
				txtHandle.Focus();
				txtHandle.SelectAll();
			}
		}
	}
}
