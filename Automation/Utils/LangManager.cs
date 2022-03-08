using System;
using System.Collections.Generic;
using System.Reflection;
using UIToolkits;

namespace Automation.Utils
{
	/// <summary>
	/// Localization
	/// </summary>
	class LangManager
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
		static LangManager()
		{
			List<LangBase> localeList = CreateAllLocales();			
			foreach (LangBase item in localeList)
			{				
				Register(item.Name, item.Contents);
			}			
		}

		/// <summary>
		/// 从程序集中创建所有继承自BaseLocale类型的对象
		/// </summary>
		/// <returns>对象列表</returns>
		static List<LangBase> CreateAllLocales()
		{
			List<LangBase> localeList = new List<LangBase>();

			Assembly[] asmList = AppDomain.CurrentDomain.GetAssemblies();
			Assembly asm = Array.Find(asmList, x => string.Compare(x.GetName().Name, "Automation", true) == 0);
			if (asm == null)
			{
				return localeList;
			}			

			Type BaseLocaleType = typeof(LangBase);
			Type[] typeList = asm.GetTypes();
			foreach (Type type in typeList)
			{
				if (!type.IsSubclassOf(BaseLocaleType))
				{
					continue;
				}

				ConstructorInfo ci = type.GetConstructor(Type.EmptyTypes);
				if (ci == null)
				{
					continue;
				}

				LangBase item = (LangBase)(ci.Invoke(new object[] { }));
				if (item == null)
				{
					continue;
				}

				localeList.Add(item);
			}

			return localeList;
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
