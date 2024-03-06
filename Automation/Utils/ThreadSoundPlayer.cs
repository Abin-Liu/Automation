using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;

namespace Automation.Utils
{
	class ThreadSoundPlayer : IDisposable
	{
		/// <summary> 
		/// Start or stop sound alarm
		/// </summary>
		public bool Alerting
		{
			get
			{
				return m_alerting;
			}

			set
			{
				if (value)
				{
					if (!m_alerting)
					{
						m_alerting = true;
						m_soundPlayerAlert.PlayLooping();
					}

				}
				else
				{
					if (m_alerting)
					{
						m_alerting = false;
						m_soundPlayerAlert.Stop();
					}
				}
			}
		}

		/// <summary>
		/// Play a short prompting beep sound
		/// </summary>
		/// <param name="start">Play start.wav is true, play stop.wav otherwise</param>
		public void Beep(bool start)
		{
			if (start)
			{
				m_soundPlayerStart.Play();
			}
			else
			{
				m_soundPlayerStop.Play();
			}
		}

		/// <summary> 
		/// Dispose the object
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary> 
		/// Release unmanaged resources such as device context
		/// </summary>
		private void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!m_disposed)
			{
				if (disposing)
				{
					Alerting = false;					
					m_soundPlayerStart.Dispose();
					m_soundPlayerStop.Dispose();
					m_soundPlayerAlert.Dispose();
				}

				m_disposed = true;
			}
		}

		private bool m_alerting = false;
		private bool m_disposed = false;
		private SoundPlayer m_soundPlayerStart = new SoundPlayer(Resources.ResourceManager.GetStream("Start"));
		private SoundPlayer m_soundPlayerStop = new SoundPlayer(Resources.ResourceManager.GetStream("Stop"));
		private SoundPlayer m_soundPlayerAlert = new SoundPlayer(Resources.ResourceManager.GetStream("Alert"));
	}
}
