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

		protected override void ThreadProc()
		{
			// 清空文本
			KeyStroke(Keys.A, ModKeys.Control);
			DelayBeforeAction(600);
			KeyStroke(Keys.Back);
			DelayBeforeAction(600);

			// 输入文字，速率10字/秒
			foreach (char ch in TEST_CONTENTS)
			{
				SendChar(ch);
				DelayBeforeAction(100);				
			}

			// 点击保存按钮
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
