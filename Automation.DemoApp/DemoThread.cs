using System;
using System.Windows.Forms;
using Automation;
using Win32API;

namespace Automation.DemoApp
{
	class DemoThread : AutomationThread
	{
		protected override IntPtr FindTargetWnd()
		{
			return FindWindow("Notepad", null);
		}

		protected override void PreStart()
		{
		}

		protected override void ThreadProc()
		{
			// Clear texts
			KeyStroke(Keys.A, Keys.Control);
			Sleep(500);
			KeyStroke(Keys.Back);
			Sleep(500);

			// Input at 10 speed of 10 chars per second			
			KeyStroke(TEST_CONTENTS, 100);

			// Click the save menu
			//DelayBeforeAction(500);
			//MouseClick(14, -13);
			//DelayBeforeAction(500);
			//MouseMove(96, 56);
			//DelayBeforeAction(800);
			//MouseClick(96, 56);

			Sleep(3500);
		}

		private static readonly string TEST_CONTENTS = "+Do not go gentle into that good night,\nOld age should burn and rave at close of day,\nRage, rage against the dying of the light.";
	}
}
