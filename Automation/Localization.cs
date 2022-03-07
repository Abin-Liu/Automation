using System;
using System.Collections.Generic;
using UIToolkits;

namespace Automation
{
	/// <summary>
	/// Localization
	/// </summary>
	class Localization
	{
		// Localization string set
		static LocaleCollection _locales = new LocaleCollection();

		/// <summary>
		/// Query for a localized string
		/// </summary>
		/// <param name="key">Query key</param>
		/// <returns>Returns the localized string, or the key itself if not exists</returns>
		public static string Get(string key)
		{
			return _locales.GetLocalizedString(key);
		}

		/// <summary>
		/// Static c'tor
		/// </summary>
		static Localization()
		{
			// Chinese simplified
			Register("zh-CN", new Dictionary<string, string>() {
				{ "Thread is already running.", "线程已经在运行中。" },
				{ "Target window not found.", "目标窗口未找到。" },
				{ "The thread is still running, exit anyway?", "线程仍在运行中，是否确定退出？" },
			});

			Register("zh-TW", new Dictionary<string, string>() {
				{ "Thread is already running.", "線程已經在運行中。" },
				{ "Target window not found.", "目標窗體未找到。" },
				{ "The thread is still running, exit anyway?", "線程仍在運行中，是否確定退出？" },
			});
		}

		/// <summary>
		/// Register a new locale
		/// </summary>
		/// <param name="name">Locale name</param>
		/// <param name="contents">String set for the locale</param>
		static void Register(string name, IDictionary<string, string> contents)
		{
			Locale locale = _locales.RegisterLocale(name);
			foreach (KeyValuePair<string, string> kv in contents)
			{
				locale[kv.Key] = kv.Value;
			}
		}		
	}
}
