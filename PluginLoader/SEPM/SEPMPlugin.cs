using System;
using HarmonyLib;
using VRage.Plugins;

namespace SEPluginManager
{
	public interface SEPMPlugin : IPlugin
	{
		void Main(Harmony harmony, Logger log);
	}
}
