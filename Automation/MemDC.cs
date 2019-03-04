using System;
using System.Drawing;
using System.Drawing.Imaging;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// Return value of PixelScanDelegate: Continue, Success, Abort
	/// </summary>
	public enum PixelScanResults { Continue, Success, Abort };

	/// <summary>
	/// Pixel scanning delegate, called for every pixel
	/// <param name="x">Pixel x coords</param>
	/// <param name="y">Pixel y coords</param>
	/// <param name="color">Pixel RGB value</param>
	/// <param name="param">Self defined callback parameter</param>
	/// <returns>Return PixelScanResults.Continue to keep scanning, PixelScanResults.Success to end successfully, PixelScanResults.Abort to abort.</returns>
	/// </summary>
	public delegate PixelScanResults PixelScanDelegate(int x, int y, int color, Object param);

	/// <summary>
	/// Return object of MemDC.ScanPixels
	/// </summary>
	public class PixelScanData
	{
		public int X { get; set; } = 0;
		public int Y { get; set; } = 0;
		public int Color { get; set; } = 0;
	}

	/// <summary>
	/// The GDI GetPixel method only suits for single pixel fetching, and will freeze 
	/// the screen if a whole region of pixels need to be scanned quickly. MemDC copies
	/// a region of pixels from screen to memory then access them, this solution improves
	/// the performance by hundreds times.
	/// </summary>
	public class MemDC
	{
		#region Public Properties
		/// <summary>
		/// Checks whether the region is valid
		/// </summary>
		public bool Valid { get { return m_graph != null; } }
		public int X { get; private set; } = 0;
		public int Y { get; private set; } = 0;
		public int Width { get; private set; } = 0;
		public int Height { get; private set; } = 0;
		#endregion

		#region C'tors
		/// <summary>
		/// Default constructor
		/// </summary>
		public MemDC()
		{
		}

		/// <summary>
		/// Constructor with a Rectangle struct
		/// <param name="boundary">The boundary of dc.</param>
		/// </summary>
		public MemDC(Rectangle boundary)
		{
			SetBoundary(boundary);
		}

		/// <summary>
		/// Constructor with a Rectangle boundary
		/// <param name="x">X coords of top-left corner of boundary.</param>
		/// <param name="y">Y coords of top-left corner of boundary.</param>
		/// <param name="width">Width of boundary.</param>
		/// <param name="height">Height of boundary.</param>
		/// </summary>
		public MemDC(int x, int y, int width, int height)
		{
			SetBoundary(x, y, width, height);
		}

		/// <summary>
		/// Specify the boundary rectangle
		/// <param name="boundary">The boundary of dc.</param>
		/// </summary>
		public void SetBoundary(Rectangle boundary)
		{
			SetBoundary(boundary.Left, boundary.Top, boundary.Width, boundary.Height);
		}
		#endregion

		#region Boundary specification
		/// <summary>
		/// Specify the boundary rectangle
		/// <param name="x">X coords of top-left corner of boundary.</param>
		/// <param name="y">Y coords of top-left corner of boundary.</param>
		/// <param name="width">Width of boundary.</param>
		/// <param name="height">Height of boundary.</param>
		/// </summary>
		public void SetBoundary(int x, int y, int width, int height)
		{
			int origWidth = Width;
			int origHeight = Height;

			X = x;
			Y = y;
			Width = Math.Max(width, 0);
			Height = Math.Max(height, 0);

			if (origWidth == Width && origHeight == Height)
			{
				return;
			}

			if (Width > 0 && Height > 0)
			{
				m_bmp = new Bitmap(Width, Height);
				m_graph = Graphics.FromImage(m_bmp);
			}
			else
			{
				m_graph = null;
				m_bmp = null;
			}			
		}

		/// <summary>
		/// Offset the boundary
		/// <param name="x">X coords.</param>
		/// <param name="y">Y coords.</param>		
		/// </summary>
		public void Offset(int x, int y)
		{
			X += x;
			Y += y;
		}

		/// <summary>
		/// Offset the boundary
		/// <param name="offset">Values to offset.</param>
		/// </summary>
		public void Offset(Point offset)
		{
			X += offset.X;
			Y += offset.Y;
		}
		#endregion

		#region Region operations
		/// <summary> 
		/// Fetch the region from screen again use previous rectangle.
		/// <returns>Return true if success, false otherwise.</returns>
		/// </summary>
		public bool Fetch()
		{
			if (m_graph == null)
			{
				return false;
			}

			m_graph.CopyFromScreen(X, Y, 0, 0, new Size(Width, Height));
			return true;
		}

		/// <summary> 
		/// Read pixel RGB value from memory block.
		/// <param name="x">X coords (relative to memory block not screen).</param>
		/// <param name="y">Y coords (relative to memory block not screen).</param>
		/// <returns>Return RGB value if success, 0 otherwise.</returns>
		/// </summary>
		public int GetPixel(int x, int y)
		{
			if (m_bmp == null)
			{
				return -1;
			}

			Color color = m_bmp.GetPixel(x, y);
			if (color.ToArgb() == -1)
			{
				return -1;
			}

			return GDI.RGB(color.R, color.G, color.B);
		}

		/// <summary> 
		/// Scan pixels in the memory block.
		/// <param name="interlace">Interlace value.</param>
		/// <param name="pixelScanCallback">Delegate.</param>
		/// <param name="param">Callback parameter.</param>
		/// <returns>Return a PixelScanData object if success, null otherwise.</returns>
		/// </summary>
		public PixelScanData ScanPixels(int interlace, PixelScanDelegate pixelScanCallback, Object param = null)
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
					PixelScanResults result = pixelScanCallback(x, y, color, param);

					if (result == PixelScanResults.Abort)
					{
						return null;
					}

					// Pixel matches
					if (result == PixelScanResults.Success)
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
		/// Save the memory block to an image file (.bmp or .jpg)
		/// <param name="filePath">Destination file path, will be overwritten if exists.</param>
		/// <param name="jpeg">True to save as jpeg format, false to save as bmp format.</param>
		/// </summary>
		public void Save(string filePath, bool jpeg = true)
		{
			if (m_bmp != null)
			{
				m_bmp.Save(filePath, jpeg ? ImageFormat.Jpeg : ImageFormat.Bmp);
			}
		}
		#endregion

		#region Private Members
		private Bitmap m_bmp = null;
		private Graphics m_graph = null;
		#endregion
	}
}
