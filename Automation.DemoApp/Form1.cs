using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Automation;
using Automation.Utils;
using UIToolkits;

namespace Automation.DemoApp
{
	public partial class Form1 : AutomationForm
	{
		DemoThread m_thread = new DemoThread();

		public Form1()
		{
			SetThread(m_thread);
			RegisterBossMode = true;
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			m_thread.EnableBeeps = true;
			RegisterMainKey(Keys.Home);
		}		

		private void btnStart_Click(object sender, EventArgs e)
		{
			StartThread();
		}

		private void btnStop_Click(object sender, EventArgs e)
		{
			StopThread();
		}

		private void btnExit_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		protected override void OnThreadStart()
		{
			base.OnThreadStart();
			btnStart.Enabled = false;
			btnStop.Enabled = true;
		}

		protected override void OnThreadStop()
		{
			base.OnThreadStop();
			MessageBoxPro.Info(this, "Thread stopped.");
			btnStart.Enabled = true;
			btnStop.Enabled = false;			
		}

		private void btnClientDC_Click(object sender, EventArgs e)
		{
			FormClientDC form = new FormClientDC();
			form.ShowDialog(this);
		}

		private void btnCursorFetch_Click(object sender, EventArgs e)
		{
			CursorFetchForm form = new CursorFetchForm();
			form.HotKey = Keys.Pause;
			form.RefWnd = Win32API.Window.FindWindow("XLMAIN", "Email format-12-1.xlsx - Excel");
			form.Message = "Press Pause key";

			if (form.ShowDialog(this) != DialogResult.OK)
			{
				return;
			}

			CursorFetchResult res = form.Result;
			string message = string.Format("{0},{1} = {2},{3},{4}", res.X, res.Y, res.R, res.G, res.B);
			MessageBox.Show(this, message);

		}
	}
}
