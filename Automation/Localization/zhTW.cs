using System;
using System.Collections.Generic;

namespace Automation.Localization
{
	/// <summary>
	/// Localization: Chinese traditional
	/// </summary>
	static class zhTW
	{
		static readonly Dictionary<string, string> Contents = new Dictionary<string, string>() {
			{ "Thread is already running.", "線程已經在運行中。" },
			{ "Target window not found.", "目標窗體未找到。" },
			{ "The thread is still running, exit anyway?", "線程仍在運行中，是否確定退出？" },
		};

		static zhTW()
		{
			Locales.Register("zh-TW", Contents);
		}
	}
}
