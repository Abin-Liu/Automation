using System;
using System.Collections.Generic;

namespace Automation.Utils
{
	/// <summary>
	/// Base class for localization
	/// </summary>
	abstract class LangBase
	{
		/// <summary>
		/// Locale name
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Text dictionary
		/// </summary>
		public abstract Dictionary<string, string> Contents { get; }
	}
}
