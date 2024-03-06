using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Automation;
using UIToolkits;

namespace Automation.DemoApp
{
	public partial class Form1 : AutomationForm
	{
		protected override Keys Hotkey => Keys.Pause;
		DemoThread m_thread = new DemoThread();

		public Form1()
		{
			SetThread(m_thread);
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e)
		{
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
			m_thread.Beep(true);
		}

		protected override void OnThreadStop()
		{
			base.OnThreadStop();					
			btnStart.Enabled = true;
			btnStop.Enabled = false;
			m_thread.Beep(false);
			MessageBox.Show(this, "Thread stopped.");
		}		
	}
}
