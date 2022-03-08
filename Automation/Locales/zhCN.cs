using System;
using System.Collections.Generic;
using Automation.Utils;

namespace Automation.Locales
{
	/// <summary>
	/// Chinese simplified
	/// </summary>
	class zhCN : LangBase
	{
		/// <summary>
		/// Local name
		/// </summary>
		public override string Name => "zh-CN";

		/// <summary>
		/// Text dictionary
		/// </summary>
		public override Dictionary<string, string> Contents => new Dictionary<string, string>() {
				{ "Thread is already running.", "线程已经在运行中。" },
				{ "Target window not found.", "目标窗口未找到。" },
				{ "The thread is still running, exit anyway?", "线程仍在运行中，是否确定退出？" },
		};
	}
}
