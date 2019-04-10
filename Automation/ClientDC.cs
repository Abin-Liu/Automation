using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using Automation.Win32API;

namespace Automation
{
	/// <summary>
	/// The GDI GetPixel method only suits for single pixel fetching, and will freeze 
	/// the screen if a whole region of pixels need to be scanned quickly. ClientDC
	/// copies a region of pixels from client area to memory then access them, this solution
	/// improves the performance by hundreds times. It can also capture pixels of directX
	/// windows.
	/// </summary>
	public class ClientDC
	{
		/// <summary>
		/// Represents an invalid color value
		/// </summary>
		public static readonly int COLOR_INVALID = 0;

		/// <summary>
		/// Rectangle represents boundary of the data block
		/// </summary>
		public Rectangle DataBlock { get; private set; }		

		/// <summary>
		/// The underlying Bitmap object
		/// </summary>
		public Bitmap Bitmap { get; private set; } = null;

		/// <summary>
		/// Create underlying graphic objects and initialize with a window
		/// </summary>
		/// <param name="targetWnd">Handle to target window</param>
		/// <param name="maxWidth">Maximum width of the underlying bitmap, 0 for width of the entire client area</param>
		/// <param name="maxHeight">Maximum width of the underlying bitmap, 0 for height of the entire client area</param>
		public virtual bool Create(IntPtr targetWnd, int maxWidth = 0, int maxHeight = 0)
		{
			if (!UpdateTargetWnd(targetWnd))
			{
				return false;
			}			

			if (maxWidth < 1)
			{
				maxWidth = ClientSize.Width;
			}

			if (maxHeight < 1)
			{
				maxHeight = ClientSize.Height;
			}			

			if (Bitmap == null || Bitmap.Width != maxWidth || Bitmap.Height != maxHeight)
			{
				Bitmap = new Bitmap(maxWidth, maxHeight);
				m_graph = Graphics.FromImage(Bitmap);
			}
			
			return true;
		}

		/// <summary>
		/// Update target window
		/// </summary>
		/// <param name="targetWnd">Handle of target window</param>
		/// <returns>Return true if targetWnd is valid, false otherwise</returns>
		protected virtual bool UpdateTargetWnd(IntPtr targetWnd)
		{
			if (targetWnd == IntPtr.Zero)
			{
				targetWnd = Window.GetDesktopWindow();
			}

			if (!Window.IsWindow(targetWnd))
			{
				return false;
			}

			Rectangle rect = Window.GetClientRect(targetWnd);
			if (rect.Width < 1 || rect.Height < 1)
			{
				return false;
			}			

			TargetWnd = targetWnd;
			ClientToScreen = Window.ClientToScreen(targetWnd);
			ClientSize = rect.Size;
			return true;
		}

		/// <summary>
		/// Release allocated graphic resources
		/// </summary>
		public virtual void Destroy()
		{
			ClientToScreen = new Point();
			ClientSize = new Size();
			DataBlock = new Rectangle();

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
		/// Capture contents of a block from the client area to memory
		/// </summary>
		/// <param name="rect">Boundary of data block</param>
		/// <returns>Return true if success, false otherwise</returns>
		public virtual bool Capture(Rectangle rect)
		{
			return Capture(rect.Left, rect.Top, rect.Width, rect.Height);
		}

		/// <summary>
		/// Capture contents from the client area to memory
		/// </summary>
		/// <param name="x">X coordinate of data block, relative to client</param>
		/// <param name="y">Y coordinate of data block, relative to client</param>
		/// <param name="width">Width of data block</param>
		/// <param name="height">Height of data block</param>
		/// <returns>Return true if success, false otherwise</returns>
		public virtual bool Capture(int x = 0, int y = 0, int width = 0, int height = 0)
		{
			if (Bitmap == null)
			{
				throw new Exception(EXCEPTION_TEXT);
			}

			// get screen coordinates
			x = Math.Max(x, 0);
			y = Math.Max(y, 0);
			int screenX = x + ClientToScreen.X;
			int screenY = y + ClientToScreen.Y;

			// make sure all pixels are inside of bitmap boundary
			if (width < 1)
			{
				width = Bitmap.Width;
			}			

			if (height < 1)
			{
				height = Bitmap.Height;
			}

			width = Math.Min(width, Bitmap.Width - x);
			height = Math.Min(height, Bitmap.Height - y);

			if (width < 1 || height < 1)
			{
				return false; // off screen
			}

			DataBlock = new Rectangle(x, y, width, height);			

			// copy from screen
			m_graph.CopyFromScreen(screenX, screenY, 0, 0, new Size(width, height));
			return true;
		}

		/// <summary> 
		/// Read pixel RGB value from data block.
		/// <param name="x">X coords (relative to client).</param>
		/// <param name="y">Y coords (relative to client).</param>
		/// <returns>Return RGB value if success, 0 otherwise.</returns>
		/// </summary>
		public virtual int GetPixel(int x, int y)
		{
			if (Bitmap == null)
			{
				throw new Exception(EXCEPTION_TEXT);
			}		

			// translate to data-block coordinates
			x -= DataBlock.Left;
			y -= DataBlock.Top;

			Color color = Bitmap.GetPixel(x, y);
			if (color.IsEmpty)
			{
				return COLOR_INVALID;
			}

			return RGB(color.R, color.G, color.B);
		}

		/// <summary>
		/// Capture single pixel and read it
		/// </summary>
		/// <param name="x">X coords (relative to client).</param>
		/// <param name="y">Y coords (relative to client).</param>
		/// <returns>Return RGB value if success, 0 otherwise.</returns>
		public virtual int CaptureAndGetPixcel(int x, int y)
		{
			if (!Capture(x, y, 1, 1))
			{
				return COLOR_INVALID;
			}

			return GetPixel(x, y);
		}

		/// <summary> 
		/// Keeps checking whether a pixel of the target window matches specified RGB values
		/// <param name="x">X coords (relative to client)</param> 
		/// <param name="y">Y coords (relative to client)</param> 
		/// <param name="color">The RGB value</param> 
		/// <param name="timeout">Maximum milliseconds before timeout, 0 to check infinitely</param>
		/// <param name="sleep">Sleep the running thread between two checks, in millisecond (minimum is 100ms) </param>
		/// <returns>Return true if the pixel matches before timeout, false otherwise</returns>
		/// </summary>
		public virtual bool WaitForPixel(int x, int y, int color, int timeout, int sleep = 200)
		{
			sleep = Math.Max(sleep, 100);
			DateTime start = DateTime.Now;

			while (CaptureAndGetPixcel(x, y) != color)
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
		/// Save the memory block to a file, image formats are automatically determined by file extension.
		/// <param name="filePath">Destination file path, will be overwritten if exists.</param>
		/// </summary>
		public virtual void Save(string filePath)
		{
			if (Bitmap == null)
			{
				throw new Exception(EXCEPTION_TEXT);
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

		#region Internal Data
		static readonly string EXCEPTION_TEXT = "ClientDC object isn't created yet."; // exception message
		protected IntPtr TargetWnd { get; private set; } = IntPtr.Zero; // Target window
		protected Point ClientToScreen { get; private set; } // Client offset of the target window
		protected Size ClientSize { get; private set; } // Size of client area of the target window
		protected Graphics m_graph = null;
		#endregion
	}
}
