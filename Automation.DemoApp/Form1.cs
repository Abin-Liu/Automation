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
	public partial class Form1 : AutomationForm
	{
		DemoThread m_thread = new DemoThread();

		public Form1()
		{
			SetThread(m_thread);
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
			base.Form_OnLoad(sender, e);
		}

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
			base.Form_OnClosing(sender, e);
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			base.Form_OnClosed(sender, e);
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
			Message("线程结束。", MessageBoxIcon.Information);
			btnStart.Enabled = true;
			btnStop.Enabled = false;			
		}
	}
}
