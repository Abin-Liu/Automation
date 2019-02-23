using System;
using System.Runtime.InteropServices;
using System.IO;

namespace Automation.Win32API
{
	public class MultiMedia
	{
		[DllImport("winmm.dll")]
		static extern int PlaySound(string pszSound, int hmod, int falgs);
		static readonly int SND_ASYNC = 0x1;
		static readonly int SND_LOOP = 0x8;
		static readonly int SND_FILENAME = 0x20000;

		public static void PlaySound(string filePath, bool loop = false, bool async = true)
		{
			StopSound();

			if (!File.Exists(filePath))
			{
				return;
			}

			int flag = SND_FILENAME;
			if (loop)
			{
				flag |= SND_LOOP;
			}

			if (async)
			{
				flag |= SND_ASYNC;
			}

			PlaySound(filePath, 0, flag);
		}

		public static void StopSound()
		{
			PlaySound(null, 0, SND_FILENAME);
		}
	}
}
