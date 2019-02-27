using System;
using System.Drawing;
using System.Drawing.Imaging;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// GDI的GetPixel方法仅适用于单个像素访问，而在一块屏幕区域内高速扫描大量像素时
	/// 会导致画面卡顿。MemDC为解决这个问题而设计，它可以从屏幕上复制一块区域到内存
	/// 中，在内存中进行像素扫描速度会提升上千倍。
	/// </summary>
	class MemDC
	{
		#region 公开属性
		/// <summary>
		/// 所定义的屏幕区域是否合法
		/// </summary>
		public bool Valid { get { return m_graph != null; } }

		/// <summary>
		/// 复制区域左上角的x坐标
		/// </summary>
		public int Left { get; private set; }

		/// <summary>
		/// 复制区域左上角的y坐标
		/// </summary>
		public int Top { get; private set; }

		/// <summary>
		/// 复制区域的宽度
		/// </summary>
		public int Width { get; private set; }

		/// <summary>
		/// 复制区域的高度
		/// </summary>
		public int Height { get; private set; }
		#endregion

		#region 构造函数
		/// <summary> 
		/// 默认构造函数
		/// </summary>
		public MemDC()
		{
		}

		/// <summary> 
		/// 构造函数
		/// <param name="rect">指定要复制的屏幕区域</param>
		/// </summary>
		public MemDC(Rectangle rect)
			: this(rect.Left, rect.Top, rect.Width, rect.Height)
		{
		}

		/// <summary> 
		/// 构造函数
		/// <param name="left">要复制的屏幕区域左上角x坐标</param>
		/// <param name="top">要复制的屏幕区域左上角y坐标</param>
		/// <param name="width">要复制的屏幕区域宽度</param>
		/// <param name="height">要复制的屏幕区域高度</param>
		/// </summary>
		public MemDC(int left, int top, int width, int height)
		{
			Left = Math.Max(0, left);
			Top = Math.Max(0, top);
			Width = Math.Max(0, width);
			Height = Math.Max(0, height);

			// 只有宽度和高度均大于0的区域才合法
			if (Width > 0 && Height > 0)
			{
				m_bmp = new Bitmap(Width, Height);
				m_graph = Graphics.FromImage(m_bmp);
			}
			else
			{
				m_bmp = null;
				m_graph = null;
			}
		}
		#endregion

		#region 区域操作
		/// <summary> 
		/// 复制屏幕区域的内容到内存中
		/// <returns>复制成功返回true，否则返回false</returns>
		/// </summary>
		public bool Update()
		{
			if (!Valid)
			{
				return false;
			}

			m_graph.CopyFromScreen(Left, Top, 0, 0, new Size(Width, Height));
			return true;
		}

		/// <summary> 
		/// 从内存块中读取像素RGB值
		/// <param name="x">像素x坐标，相对于内存块而非屏幕</param>
		/// <param name="y">像素y坐标，相对于内存块而非屏幕</param>
		/// <returns>像素RGB值</returns>
		/// </summary>
		public int GetPixel(int x, int y)
		{
			if (m_bmp == null)
			{
				return -1;
			}

			Color color = m_bmp.GetPixel(x, y);
			return GDI.RGB(color.R, color.G, color.B);
		}

		/// <summary> 
		/// 将内存块内容保存为图像文件
		/// <param name="filePath">所要保存为的文件路径</param>
		/// <param name="jpeg">如果true，文件将被有损压缩并保存为jpeg格式，否则保存为bmp无损格式</param>
		/// </summary>
		public void Save(string filePath, bool jpeg = true)
		{
			if (m_bmp != null)
			{
				m_bmp.Save(filePath, jpeg ? ImageFormat.Jpeg : ImageFormat.Bmp);
			}
		}
		#endregion

		#region 私有成员
		private Bitmap m_bmp = null;
		private Graphics m_graph = null;
		#endregion
	}
}
