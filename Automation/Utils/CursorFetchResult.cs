using System;

namespace Automation.Utils
{
	/// <summary>
	/// 鼠标点捕捉结果
	/// </summary>
	public class CursorFetchResult
	{
		/// <summary>
		/// 鼠标位置X（如果参照窗口为IntPtr.Zero则相对于屏幕，否则相对于参照窗口的客户端区域）
		/// </summary>
		public int X { get; set; }

		/// <summary>
		/// 鼠标位置Y（如果TargetWnd为IntPtr.Zero则相对于屏幕，否则相对于TargetWnd的Client-Area）
		/// </summary>
		public int Y { get; set; }

		/// <summary>
		/// 鼠标位置像素颜色值
		/// </summary>
		public int Color { get; set; }

		/// <summary>
		/// 像素R值
		/// </summary>
		public byte R => MemDC.GetRValue(Color);

		/// <summary>
		/// 像素G值
		/// </summary>
		public byte G => MemDC.GetGValue(Color);

		/// <summary>
		/// 像素R值
		/// </summary>
		public byte B => MemDC.GetBValue(Color);

		/// <summary>
		/// 参照窗口句柄
		/// </summary>
		public IntPtr RefWnd { get; set; }
	}
}
