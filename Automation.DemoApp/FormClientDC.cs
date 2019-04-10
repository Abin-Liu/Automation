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
		ClientDC m_dc = new ClientDC();

		public FormClientDC()
		{
			InitializeComponent();
		}

		private void btnCapture_Click(object sender, EventArgs e)
		{
			string text = txtHandle.Text.Trim();
			try
			{
				int value = Convert.ToInt32(text, 16);
				m_dc.Create((IntPtr)value, pictureBox1.Width, pictureBox1.Height);
				m_dc.Capture(100, 100, 100, 100);
				pictureBox1.Image = m_dc.Bitmap;
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
