using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Automation
{
	/// <summary>
	/// Key-value map for locale strings
	/// </summary>
	public sealed class Locale
	{
		/// <summary>
		/// Get or set locale values by key
		/// </summary>
		public string this[string key]
		{
			get
			{
				if (string.IsNullOrEmpty(key))
				{
					return null;
				}

				string value = null;
				m_map.TryGetValue(key, out value);
				return value;
			}

			set
			{
				if (string.IsNullOrEmpty(key))
				{
					return;
				}

				if (m_map.ContainsKey(key))
				{
					if (value == null)
					{
						// remove entry
						m_map.Remove(key);
					}
					else
					{
						// change value
						m_map[key] = value;
					}
				}
				else
				{
					if (value == null)
					{
						// make no sense
					}
					else
					{
						// add key
						m_map.Add(key, value);
					}
				}
			}
		}

		// the underlying dictonary
		private Dictionary<string, string> m_map = new Dictionary<string, string>();
	}

	public sealed class LocaleCollection
	{

		/// <summary>
		/// Get system locale: en, zh-CN, zh-TW, etc
		/// </summary>
		public static string SystemLocale { get; } = System.Globalization.CultureInfo.InstalledUICulture.Name;		

		/// <summary>
		/// Register a locale if not exists
		/// <param name="name">Name of the locale, must comply with System.Globalization.CultureInfo</param>
		/// <returns>The locale object</returns>
		/// </summary>
		public Locale RegisterLocale(string name)
		{
			if (string.IsNullOrEmpty(name) || name == "en")
			{
				return null;
			}

			Locale locale = null;
			if (m_map.TryGetValue(name, out locale))
			{
				return locale;
			}

			locale = new Locale();
			m_map.Add(name, locale);
			return locale;
		}

		/// <summary>
		/// Retrieve a localized string in current system locale
		/// <param name="key">Key of localized string</param>
		/// <returns>The locale object</returns>
		/// </summary>
		public string GetLocalizedString(string key)
		{
			if (SystemLocale == "en")
			{
				return key;
			}

			Locale locale = null;
			if (!m_map.TryGetValue(SystemLocale, out locale))
			{
				return key;
			}

			return locale[key];
		}

		// the underlying dictonary
		private Dictionary<string, Locale> m_map = new Dictionary<string, Locale>();
	}
}
