////////////////////////////////////////////////
// MemDC
//
// Abin Liu
// 2018-4-15
////////////////////////////////////////////////

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

namespace Automation
{
	/// <summary>
	/// The GDI GetPixel method only suits for single pixel fetching, and will freeze 
	/// the screen if a whole region of pixels need to be scanned quickly. ClientDC
	/// copies a region of pixels from client area to memory then access them, this solution
	/// improves the performance by hundreds times. It can also capture pixels of directX
	/// windows.
	/// </summary>
	public class MemDC : IDisposable
	{
		/// <summary>
		/// Represents an invalid color value, 0xffffffff in Win32
		/// </summary>
		public const int COLOR_INVALID = -1;

		/// <summary>
		/// The underlying Bitmap object
		/// </summary>
		public Bitmap Bitmap { get; private set; } = null;

		/// <summary>
		/// Checks whether the object is valid
		/// </summary>
		public bool Valid { get { return m_graph != null; } }

		/// <summary>
		/// Width of the underlying bitmap
		/// </summary>
		public int Width { get { return Bitmap == null ? 0 : Bitmap.Width; } }

		/// <summary>
		/// Height of the underlying bitmap
		/// </summary>
		public int Height { get { return Bitmap == null ? 0 : Bitmap.Height; } }		

		/// <summary>
		/// Dispose the object
		/// </summary>
		public virtual void Dispose()
		{
			Cleanup();
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Free resource
		/// </summary>
		public virtual void Cleanup()
		{
			if (m_graph != null)
			{
				m_graph.Dispose();
				m_graph = null;
			}

			if (Bitmap != null)
			{
				Bitmap.Dispose();
				Bitmap = null;
			}
		}

		/// <summary>
		/// Capture a block of pixels from screen to memory
		/// </summary>
		/// <param name="rect">Boundary of data block</param>
		/// <returns>Return true if success, false otherwise</returns>
		public virtual bool Capture(Rectangle rect)
		{
			return Capture(rect.Left, rect.Top, rect.Width, rect.Height);
		}

		/// <summary>
		/// Capture a block of pixels from screen to memory
		/// </summary>
		/// <param name="x">X coord of screen</param>
		/// <param name="y">Y coord of screen</param>
		/// <param name="width">Width of data block</param>
		/// <param name="height">Height of data block</param>
		/// <returns>Return true if success, false otherwise</returns>
		public virtual bool Capture(int x, int y, int width, int height)
		{
			if (width < 1 || height < 1)
			{
				return false;
			}

			try
			{
				if (Bitmap == null || Bitmap.Width != width || Bitmap.Height != height)
				{
					Cleanup();
					Bitmap = new Bitmap(width, height);
					m_graph = Graphics.FromImage(Bitmap);
				}

				x = Math.Max(x, 0);
				y = Math.Max(y, 0);

				// copy from screen
				m_graph.CopyFromScreen(x, y, 0, 0, new Size(width, height));
				return true;
			}
			catch (ThreadAbortException e)
			{
				throw e;
			}
			catch
			{
				return false;
			}			
		}

		/// <summary> 
		/// Read pixel RGB value from data block.
		/// <param name="x">X coords of mem dc.</param>
		/// <param name="y">Y coords of mem dc.</param>
		/// <returns>Return RGB value if success, COLOR_INVALID otherwise.</returns>
		/// </summary>
		public virtual int GetPixel(int x, int y)
		{
			if (Bitmap == null)
			{
				return COLOR_INVALID;
			}
			
			if (x < 0 || y < 0 || x >= Bitmap.Width || y >= Bitmap.Height)
			{
				return COLOR_INVALID;
			}			

			try
			{
				Color color = Bitmap.GetPixel(x, y);
				if (color.IsEmpty)
				{
					return COLOR_INVALID;
				}
				return RGB(color.R, color.G, color.B);
			}
			catch (ThreadAbortException e)
			{
				throw e;
			}
			catch
			{
				return COLOR_INVALID;
			}
		}

		/// <summary>
		/// Capture single pixel and read it
		/// </summary>
		/// <param name="x">X coords of screen.</param>
		/// <param name="y">Y coords of screen.</param>
		/// <returns>Return RGB value if success, COLOR_INVALID otherwise.</returns>
		public virtual int CaptureAndGetPixel(int x, int y)
		{
			if (!Capture(x, y, 1, 1))
			{
				return COLOR_INVALID;
			}

			return GetPixel(0, 0);
		}

		/// <summary>
		/// Check pixel color with tolerances
		/// </summary>
		/// <param name="color">Color to be checked</param>
		/// <param name="rangeR">Tolerance range [min, max] for R value, null to ignore R value check</param>
		/// <param name="rangeG">Tolerance range [min, max] for G value, null to ignore G value check</param>
		/// <param name="rangeB">Tolerance range [min, max] for B value, null to ignore B value check</param>
		/// <returns>Return true if the pixel passed, false otherwise</returns>
		public static bool ExamPixel(int color, int[] rangeR, int[] rangeG, int[] rangeB)
		{
			int r = GetRValue(color);
			int g = GetGValue(color);
			int b = GetBValue(color);

			if (rangeR != null && (r < rangeR[0] || r > rangeR[1]))
			{
				return false;
			}

			if (rangeG != null && (g < rangeG[0] || g > rangeG[1]))
			{
				return false;
			}

			if (rangeB != null && (b < rangeB[0] || b > rangeB[1]))
			{
				return false;
			}

			return true;
		}		

		/// <summary> 
		/// Keeps checking whether a pixel of the target window matches specified RGB values
		/// <param name="x">X coords of screen</param> 
		/// <param name="y">Y coords of screen</param> 
		/// <param name="color">The RGB value</param> 
		/// <param name="timeout">Maximum milliseconds before timeout, 0 to check infinitely</param>
		/// <param name="sleep">Sleep the running thread between two checks, in millisecond (minimum is 100ms) </param>
		/// <returns>Return true if the pixel matches before timeout, false otherwise</returns>
		/// </summary>
		public virtual bool WaitForPixel(int x, int y, int color, int timeout, int sleep = 200)
		{
			sleep = Math.Max(sleep, 100);
			DateTime start = DateTime.Now;

			while (CaptureAndGetPixel(x, y) != color)
			{
				if (timeout > 0 && (DateTime.Now - start).TotalMilliseconds > timeout)
				{
					return false;
				}

				Thread.Sleep(sleep);
			}

			return true;
		}

		/// <summary>
		/// Delegate used for pixel scanning
		/// </summary>
		/// <param name="x">The x coords relative to bitmap</param>
		/// <param name="y">The y coords relative to bitmap</param>
		/// <param name="pixel">RGB value of the pixel at (x,y)</param>
		/// <param name="parameter">Callback parameter</param>
		/// <returns>Return true to abort the scanning, false to proceed with next pixel</returns>
		public delegate bool ScanPixelDelegate(int x, int y, int pixel, object parameter);

		/// <summary>
		/// Scan pixels
		/// </summary>
		/// <param name="callback">Callback function</param>
		/// <param name="parameter">Callback parameter</param>
		/// <param name="interlace">Scanning interlace for both horizontal and vertical, 0 means scan every pixel</param>
		/// <returns>Returns true if the last callback returned true, or false if all pixels have been scanned</returns>
		public bool ScanPixels(ScanPixelDelegate callback, object parameter = null, int interlace = 0)
		{
			if (callback == null)
			{
				throw new ArgumentNullException("callback");
			}

			if (!Valid)
			{
				throw new NullReferenceException("Bitmap not captured yet.");
			}

			if (interlace < 0)
			{
				interlace = 0;
			}

			int width = Width;
			int height = Height;
			interlace++;

			for (int x = 0; x < width; x += interlace)
			{
				for (int y = 0; y < height; y += interlace)
				{
					int pixel = GetPixel(x, y);
					if (callback(x, y, pixel, parameter))
					{
						return true;
					}
				}
			}

			return false;
		}

		/// <summary> 
		/// Save the memory block to a file, image formats are automatically determined by file extension.
		/// <param name="filePath">Destination file path, will be overwritten if exists.</param>
		/// </summary>
		public virtual void Save(string filePath)
		{
			if (Bitmap == null)
			{
				return;
			}

			if (string.IsNullOrEmpty(filePath))
			{
				return;
			}

			int pos = filePath.LastIndexOf('.');
			if (pos == -1)
			{
				return;
			}

			ImageFormat format;

			string ext = filePath.Substring(pos).Trim().ToLower();
			switch (ext)
			{
				case "gif":
					format = ImageFormat.Gif;
					break;

				case "ico":
					format = ImageFormat.Icon;
					break;

				case "png":
					format = ImageFormat.Png;
					break;

				case "tiff":
					format = ImageFormat.Tiff;
					break;

				case "jpg":
				case "jpeg":
					format = ImageFormat.Jpeg;
					break;

				default:
					format = ImageFormat.Bmp;
					break;
			}

			Bitmap.Save(filePath, format);
		}

		/// <summary>
		/// Compose rgb values into an integer
		/// </summary>
		/// <param name="r">Value of r component</param>
		/// <param name="g">Value of g component</param>
		/// <param name="b">Value of b component</param>
		/// <returns>Integer form of rgb value</returns>
		public static int RGB(byte r, byte g, byte b)
		{
			return ((int)r << 16) | ((short)g << 8) | b;
		}

		/// <summary>
		/// Compose rgb values into an integer, unlike System.Drawing.Color, it eliminates alpha value
		/// </summary>
		/// <param name="color">Value of color</param>		
		/// <returns>Integer form of rgb value</returns>
		public static int RGB(Color color)
		{
			return RGB(color.R, color.G, color.B);
		}

		/// <summary>
		/// Extract the r component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the r component</returns>
		public static byte GetRValue(int color)
		{
			return (byte)(color >> 16);
		}

		/// <summary>
		/// Extract the g component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the g component</returns>
		public static byte GetGValue(int color)
		{
			return (byte)(((short)color) >> 8);
		}

		/// <summary>
		/// Extract the b component from an integer grb value
		/// </summary>
		/// <param name="color">Integer form of rgb value</param>
		/// <returns>Value of the b component</returns>
		public static byte GetBValue(int color)
		{
			return (byte)color;
		}

		// Private members
		private Graphics m_graph = null; // The underlysing Graphics object
	}
}
