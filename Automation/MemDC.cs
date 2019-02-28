using System;
using System.Drawing;
using System.Drawing.Imaging;
using Automation.Win32API;

namespace Automation
{
	public delegate int ScanPixelDelegate(int x, int y, int color, Object param); // 返回值0=Continue, 1=Success, 2=Abort
	
	/// <summary>
	/// GDI的GetPixel方法仅适用于单个像素访问，而在一块屏幕区域内高速扫描大量像素时
	/// 会导致画面卡顿。MemDC为解决这个问题而设计，它可以从屏幕上复制一块区域到内存
	/// 中，在内存中进行像素扫描速度会提升上千倍。
	/// </summary>
	public class MemDC
	{
		#region 公开属性
		/// <summary>
		/// 所定义的屏幕区域是否合法
		/// </summary>
		public bool Valid { get { return Width > 0 && Height > 0; } }
		public int Left { get; private set; } = 0;
		public int Top { get; private set; } = 0;
		public int Width { get; private set; } = 0;
		public int Height { get; private set; } = 0;
		#endregion

		#region 区域操作
		/// <summary> 
		/// 重新复制屏幕指定区域的内容到内存中
		/// <returns>复制成功返回true，否则返回false</returns>
		/// </summary>
		public bool Fetch()
		{
			if (Width < 1 || Height < 1)
			{
				return false;
			}

			m_graph.CopyFromScreen(Left, Top, 0, 0, new Size(Width, Height));
			return true;
		}

		/// <summary> 
		/// 复制屏幕指定区域的内容到内存中
		/// <param name="rect">要复制的屏幕区域</param>
		/// <returns>复制成功返回true，否则返回false</returns>
		/// </summary>
		public bool Fetch(Rectangle rect)
		{			
			return Fetch(rect.X, rect.Y, rect.Width, rect.Height);
		}

		/// <summary> 
		/// 复制屏幕指定区域的内容到内存中
		/// <param name="x">要复制的屏幕区域左上角x坐标</param>
		/// <param name="y">要复制的屏幕区域左上角y坐标</param>
		/// <param name="width">要复制的屏幕区域宽度</param>
		/// <param name="height">要复制的屏幕区域高度</param>
		/// <returns>复制成功返回true，否则返回false</returns>
		/// </summary>
		public bool Fetch(int x, int y, int width, int height)
		{
			if (width < 1 || height < 1)
			{
				return false;
			}

			Left = Math.Max(x, 0);
			Top = Math.Max(y, 0);

			// 如果宽度或高度改变，需要重新创建bitmap
			if (width != Width || height != Height)
			{
				Width = width;
				Height = height;
				m_bmp = new Bitmap(width, height);
				m_graph = Graphics.FromImage(m_bmp);
			}

			return Fetch();
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
		/// 扫描内存块中的像素RGB值
		/// <param name="interlace">隔行值</param>
		/// <param name="scanPixelCallback">回调函数</param>
		/// <param name="param">回调参数</param>
		/// <returns>如果像素被找到返回一个PixelScanData对象，否则返回null</returns>
		/// </summary>
		public PixelScanData ScanPixels(int interlace, ScanPixelDelegate scanPixelCallback, Object param = null)
		{
			if (!Valid)
			{
				return null;
			}

			if (interlace < 1)
			{
				interlace = 1;
			}

			for (int x = 0; x < Width; x += interlace)
			{
				for (int y = 0; y < Height; y += interlace)
				{
					int color = GetPixel(x, y);
					int result = scanPixelCallback(x, y, color, param);

					if (result == PixelScanData.Abort)
					{
						return null;
					}

					// pixel found
					if (result == PixelScanData.Success)
					{
						PixelScanData data = new PixelScanData();
						data.X = x;
						data.Y = y;
						data.Color = color;
						return data;
					}
				}
			}

			return null;
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

	/// <summary>
	/// 用以储存MemDC.ScanPixels方法的扫描结果
	/// </summary>
	public class PixelScanData
	{
		public static readonly int Continue = 0;
		public static readonly int Success = 1;
		public static readonly int Abort = 2;
		public int X { get; set; }
		public int Y { get; set; }
		public int Color { get; set; }
	}
}
