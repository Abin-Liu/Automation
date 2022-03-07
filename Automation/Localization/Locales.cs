using System;
using System.Collections.Generic;
using UIToolkits;

namespace Automation.Localization
{
	/// <summary>
	/// Localization
	/// </summary>
	static class Locales
	{
		// Localization string set
		static LocaleCollection _locales = new LocaleCollection();

		/// <summary>
		/// Register a new locale
		/// </summary>
		/// <param name="name">Locale name</param>
		/// <param name="contents">String set for the locale</param>
		public static void Register(string name, IDictionary<string, string> contents)
		{
			Locale locale = _locales.RegisterLocale(name);
			foreach (KeyValuePair<string, string> kv in contents)
			{
				locale[kv.Key] = kv.Value;
			}
		}

		/// <summary>
		/// Query for a localized string
		/// </summary>
		/// <param name="key">Query key</param>
		/// <returns>Returns the localized string, or the key itself if not exists</returns>
		public static string Get(string key)
		{
			return _locales.GetLocalizedString(key);
		}
	}
}
