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
	/// Return object of ClientBmp.ScanPixels
	/// </summary>
	public class PixelScanData
	{
		public int X { get; set; } = 0;
		public int Y { get; set; } = 0;
		public int Color { get; set; } = 0;
	}

	/// <summary>
	/// The GDI GetPixel method only suits for single pixel fetching, and will freeze 
	/// the screen if a whole region of pixels need to be scanned quickly. ClientBmp copies
	/// a region of pixels from screen to memory then access them, this solution improves
	/// the performance by hundreds times.
	/// </summary>
	public class ClientBmp
	{
		#region Public Properties

		/// <summary>
		/// Checks whether the object is valid
		/// </summary>
		public bool Valid { get { return m_graph != null; } }

		/// <summary>
		/// The boundary area, relative to client area of the target window
		/// </summary>
		public Rectangle Boundary { get; private set; }

		/// <summary>
		/// Handle of the target window
		/// </summary>
		public IntPtr TargetWnd { get; private set; } = IntPtr.Zero;
		#endregion

		#region Boundary specification
		/// <summary>
		/// Specify the target window of which the entire client area will be fetched
		/// <param name="targetWnd">Handle of the target window.</param>
		/// <returns>Return true if success, false otherwise</returns>
		/// </summary>
		public bool SetBoundary(IntPtr targetWnd)
		{
			if (targetWnd == IntPtr.Zero)
			{
				targetWnd = Window.GetDesktopWindow();
			}

			return SetBoundary(targetWnd, Window.GetClientRect(targetWnd));
		}

		/// <summary>
		/// Specify the target window and area to be fetched
		/// <param name="targetWnd">Handle of the target window.</param>
		/// <param name="boundary">The boundary of area to be fetched, relative to client area of the target window.</param>
		/// <returns>Return true if success, false otherwise</returns>
		/// </summary>
		public bool SetBoundary(IntPtr targetWnd, Rectangle boundary)
		{
			if (targetWnd == IntPtr.Zero)
			{
				targetWnd = Window.GetDesktopWindow();
			}

			if (targetWnd != TargetWnd || boundary != Boundary)
			{
				TargetWnd = targetWnd;
				Boundary = boundary;

				m_bmp = new Bitmap(boundary.Width, boundary.Height);
				m_graph = Graphics.FromImage(m_bmp);
			}

			return Valid;			
		}

		/// <summary>
		/// Specify the target window and area to be fetched
		/// <param name="targetWnd">Handle of the target window.</param>
		/// <param name="x">X coords of top-left corner of boundary, relative to client area of the target window.</param>
		/// <param name="y">Y coords of top-left corner of boundary, relative to client area of the target window.</param>
		/// <param name="width">Width of boundary.</param>
		/// <param name="height">Height of boundary.</param>
		/// <returns>Return true if success, false otherwise</returns>
		/// </summary>
		public bool SetBoundary(IntPtr targetWnd, int x, int y, int width, int height)
		{
			return SetBoundary(targetWnd, new Rectangle(x, y, width, height));
		}

		/// <summary>
		/// Offset the boundary
		/// <param name="x">X coords.</param>
		/// <param name="y">Y coords.</param>		
		/// </summary>
		public void Offset(int x, int y)
		{
			Boundary.Offset(x, y);
		}

		/// <summary>
		/// Offset the boundary
		/// <param name="offset">Values to offset.</param>
		/// </summary>
		public void Offset(Point offset)
		{
			Boundary.Offset(offset);
		}
		#endregion

		#region Region operations
		/// <summary> 
		/// Fetch the region from screen again use previous rectangle.
		/// <returns>Return true if success, false otherwise.</returns>
		/// </summary>
		public bool Fetch()
		{
			if (!Valid)
			{
				return false;
			}

			Point point = new Point(Boundary.Left, Boundary.Top);
			Point offset = Window.ClientToScreen(TargetWnd);
			point.Offset(offset);
			m_graph.CopyFromScreen(point.X, point.Y, 0, 0, new Size(Boundary.Width, Boundary.Height));
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
			if (!Valid)
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

			int width = Boundary.Width;
			int height = Boundary.Height;

			for (int x = 0; x < width; x += interlace)
			{
				for (int y = 0; y < height; y += interlace)
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
						// x and y are zero-based and relative to boundary
						PixelScanData data = new PixelScanData();
						data.X = x + Boundary.Left;
						data.Y = y + Boundary.Top;						
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
			if (Valid)
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
