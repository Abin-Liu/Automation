using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Automation;
using Win32API;
using MFGLib;

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
			IntPtr hwnd;
			string text = txtHandle.Text.Trim();
			try
			{
				int value = Convert.ToInt32(text, 16);
				hwnd = (IntPtr)value;
			}
			catch
			{
				hwnd = Window.GetDesktopWindow();
			}

			Rectangle rect = Window.GetWindowRect(hwnd);
			MemDC dc = new MemDC();
			dc.Capture(rect.Left, rect.Top, 100, 100);
			int pixel = dc.GetPixel(80, 80);
			pictureBox1.Image = dc.Bitmap;
		}
	}
}
