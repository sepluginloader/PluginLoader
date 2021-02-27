using System;
using avaness.PluginLoader;
using HarmonyLib;

namespace SEPluginManager
{
	public class Logger
	{
		private readonly LogFile log;
		private readonly string prefix;

		public Logger(string prefix, LogFile log)
		{
			this.prefix = prefix;
			this.log = log;
		}

		public void Log(string text)
		{
			log.WriteLine(text, prefix);
		}

	}
}
