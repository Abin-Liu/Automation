using System;
using System.Windows.Forms;
using Automation;
using Win32API;

namespace Automation.DemoApp
{
	class DemoThread : AutomationThread
	{
		public override IntPtr FindTargetWnd()
		{
			return FindWindow("Notepad", null);
		}

		protected override void ThreadProc()
		{
			// Clear texts
			KeyStroke(Keys.A, ModKeys.Control);
			DelayBeforeAction(600);
			KeyStroke(Keys.Back);
			DelayBeforeAction(600);

			// Input at 10 speed of 10 chars per second
			SendString(TEST_CONTENTS, 100);

			// Click the save menu
			DelayBeforeAction(500);
			MouseClick(14, -13);
			DelayBeforeAction(500);
			MouseMove(96, 56);
			DelayBeforeAction(800);
			MouseClick(96, 56);			
		}

		private static readonly string TEST_CONTENTS = "Do not go gentle into that good night,\nOld age should burn and rave at close of day,\nRage, rage against the dying of the light.";
	}
}
