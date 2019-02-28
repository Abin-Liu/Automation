using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Automation;

namespace Automation.DemoApp
{
	class DemoThread : AutomationThread
	{
		public DemoThread()
		{
			TargetWndClass = "Notepad";
		}

		static readonly string CONTENTS = "Do not go gentle into that good night,\nOld age should burn and rave at close of day,\nRage, rage against the dying of the light.";		

		protected override void ThreadProc()
		{			
			KeyStroke(Keys.A, ModKeys.Control);
			DelayBeforeAction(600);
			KeyStroke(Keys.Back);
			DelayBeforeAction(600);

			foreach (char ch in CONTENTS)
			{
				SendChar(ch);
				DelayBeforeAction(100);				
			}

			DelayBeforeAction(500);
			LeftClick(14, -13);
			DelayBeforeAction(500);
			MouseMove(96, 56);
			DelayBeforeAction(800);
			LeftClick(96, 56);
		}
	}
}
