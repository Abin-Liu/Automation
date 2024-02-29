using System;

namespace Automation
{
	/// <summary>
	/// Target window auto foregrounding mode
	/// </summary>
	public enum ForegroundModes
	{
		/// <summary>
		/// Never auto foregrounding
		/// </summary>
		Never,

		/// <summary>
		/// Always auto foregrounding
		/// </summary>
		Always,

		/// <summary>
		/// Only auto foregrounding when cursor is in client rectangle
		/// </summary>
		CursorInClient,		
	}
}
