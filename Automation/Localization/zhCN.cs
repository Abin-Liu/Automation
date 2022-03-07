using System;
using System.Collections.Generic;

namespace Automation.Localization
{
	/// <summary>
	/// Localization: Chinese simplified
	/// </summary>
	static class zhCN
	{
		static readonly Dictionary<string, string> Contents = new Dictionary<string, string>() {
			{ "Thread is already running.", "线程已经在运行中。" },
			{ "Target window not found.", "目标窗口未找到。" },
			{ "The thread is still running, exit anyway?", "线程仍在运行中，是否确定退出？" },
		};

		static zhCN()
		{
			Locales.Register("zh-CN", Contents);
		}
	}
}
