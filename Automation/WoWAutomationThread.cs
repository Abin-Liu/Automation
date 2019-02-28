using System;
using System.Drawing;
using System.Windows.Forms;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// 继承自AutomationThread的抽象线程类，封装了大部分与WoW窗口交互的通用方法和常量
	/// </summary>
	public abstract class WoWAutomationThread : AutomationThread
	{
		#region 常量定义
		/// <summary>
		/// 信号像素颜色定义
		/// </summary>
		public static readonly int Invalid = -1;
		public static readonly int Red = 0xff0000;
		public static readonly int Green = 0x00ff00;
		public static readonly int Blue = 0x0000ff;
		public static readonly int Yellow = 0xffff00;
		public static readonly int Cyan = 0x00ffff;
		public static readonly int Purple = 0xff00ff;

		/// <summary>
		/// 信号像素在客户端的通常位置
		/// </summary>
		public enum ClientPosition { Invalid = -1, TopLeft, Top, TopRight, Right, BottomRight, Bottom, BottomLeft, Left, Center };
		#endregion

		#region 公开方法
		/// <summary>
		/// 是否需要AntiIdle
		/// </summary>
		public bool NeedAntiIdle { get { return (DateTime.Now - m_lastAntiIdle).TotalSeconds > 30; } }

		/// <summary>
		/// 寻求组队UI是否已出现
		/// </summary>
		public bool HasLFDUI { get { return GetPixel(ClientPosition.Center) == Blue; } }

		/// <summary>
		/// WoW内报警UI是否已出现
		/// </summary>
		public bool HasAlertUI { get { return GetPixel(ClientPosition.Center) == Purple; } }

		/// <summary>
		/// WoW是否当前处于窗口模式
		/// </summary>
		public bool IsWindowMode
		{
			get
			{
				Rectangle gameRect = GetClientRect();
				Rectangle desktopRect = Window.GetClientRect(Window.GetDesktopWindow());
				return gameRect.Width < desktopRect.Width || gameRect.Height < desktopRect.Height;
			}
		}
		#endregion

		#region 构造函数
		/// <summary>
		/// 默认构造函数
		/// </summary>
		public WoWAutomationThread()
		{
			TargetWndClass = "GxWindowClass"; // WOW窗口类
		}
		#endregion

		#region 扩展方法
		/// <summary>
		/// GetPixel方法扩展，以信号像素位置为参数
		/// <param name="position">信号像素位置</param>
		/// <returns>像素RGB值</returns>
		/// </summary>
		public int GetPixel(ClientPosition position)
		{
			Point point = TranslatePosition(position);
			return GetPixel(point.X, point.Y);
		}

		/// <summary>
		/// 将信号像素位置转换为客户端坐标值
		/// <param name="position">信号像素位置</param>
		/// <returns>客户端坐标值</returns>
		/// </summary>
		public Point TranslatePosition(ClientPosition position)
		{
			Rectangle rect = GetClientRect();
			Point point = new Point(0, 0);
			switch (position)
			{
				case ClientPosition.TopLeft:
					point.X = rect.Left;
					point.Y = rect.Top;
					break;

				case ClientPosition.Top:
					point.X = rect.Left + rect.Width / 2;
					point.Y = rect.Top;
					break;

				case ClientPosition.TopRight:
					point.X = rect.Right - 1;
					point.Y = rect.Top;
					break;

				case ClientPosition.Right:
					point.X = rect.Right - 1;
					point.Y = rect.Top + rect.Height / 2;
					break;

				case ClientPosition.BottomRight:
					point.X = rect.Right - 1;
					point.Y = rect.Bottom - 1;
					break;

				case ClientPosition.Bottom:
					point.X = rect.Left + rect.Width / 2;
					point.Y = rect.Bottom - 1;
					break;

				case ClientPosition.BottomLeft:
					point.X = rect.Left;
					point.Y = rect.Bottom - 1;
					break;

				case ClientPosition.Left:
					point.X = rect.Left;
					point.Y = rect.Top + rect.Height / 2;
					break;

				case ClientPosition.Center:
					point.X = rect.Left + rect.Width / 2;
					point.Y = rect.Top + rect.Height / 2;
					break;

				default:					
					break;
			}

			return point;
		}

		/// <summary>
		/// 检查像素颜色是否为信号像素通用RGB值
		/// <param name="color">像素颜色</param>
		/// <returns>如果像素颜色符合则返回true，否则返回false</returns>
		/// </summary>
		public static bool IsKnownPixel(int color)
		{			
			return color == Red
				|| color == Green
				|| color == Blue
				|| color == Yellow
				|| color == Purple
				|| color == Cyan;
		}

		/// <summary>
		/// 以当前鼠标位置为idle位置，后续HideCursor方法将把鼠标移动到此处
		/// </summary>
		public void SetIdlePoint()
		{
			Point cursor = Input.GetCursorPos();
			Point offset = Window.ScreenToClient(TargetWnd);
			cursor.Offset(offset);

			Rectangle rect = GetClientRect();
			if (!rect.Contains(cursor))
			{
				cursor.X = rect.Left + rect.Width / 2 + 300;
				cursor.Y = rect.Top + rect.Height / 2 + 300;
			}
			m_idlePoint = cursor;
		}

		/// <summary>
		/// 将把鼠标移动到前面调用SetIdlePoint时的位置
		/// </summary>
		public void HideCursor()
		{			
			MouseMove(m_idlePoint.X, m_idlePoint.Y);
		}

		/// <summary>
		/// 向WoW窗口发送空格键并记录当前时间
		/// </summary>
		public void AntiIdle()
		{
			KeyStroke(Keys.Space);
			m_lastAntiIdle = DateTime.Now;
		}
		#endregion

		#region 私有成员
		private DateTime m_lastAntiIdle = DateTime.Now;
		Point m_idlePoint = new Point(0, 0);
		#endregion
	}
}
