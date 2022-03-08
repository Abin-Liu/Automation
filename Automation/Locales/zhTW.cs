using System;
using System.Collections.Generic;
using Automation.Utils;

namespace Automation.Locales
{
	/// <summary>
	/// Chinese traditional
	/// </summary>
	class zhTW : LangBase
	{
		/// <summary>
		/// Local name
		/// </summary>
		public override string Name => "zh-TW";

		/// <summary>
		/// Text dictionary
		/// </summary>
		public override Dictionary<string, string> Contents => new Dictionary<string, string>() {
			{ "Thread is already running.", "線程已經在運行中。" },
			{ "Target window not found.", "目標窗體未找到。" },
			{ "The thread is still running, exit anyway?", "線程仍在運行中，是否確定退出？" },
		};
	}
}
