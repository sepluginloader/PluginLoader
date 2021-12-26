using HarmonyLib;
using System;
using VRage.Game;
using System.Linq;
using avaness.PluginLoader.Data;
using System.Collections.Generic;
using Sandbox.Definitions;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyDefinitionManager), "LoadData")]
    public static class Patch_MyDefinitionManager
    {

        public static void Prefix(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {

            try
            {
                HashSet<ulong> currentMods = new HashSet<ulong>(mods.Select(x => x.PublishedFileId));
                List<MyObjectBuilder_Checkpoint.ModItem> newMods = new List<MyObjectBuilder_Checkpoint.ModItem>(mods);

                PluginList list = Main.Instance.List;
                foreach (string id in Main.Instance.Config.EnabledPlugins)
                {
                    PluginData data = list[id];
                    if (data is ModPlugin mod && !currentMods.Contains(mod.WorkshopId) && mod.Exists)
                    {
                        LogFile.WriteLine("Loading client mod definitions for " + mod.WorkshopId);
                        newMods.Add(mod.GetModItem());
                    }
                }

                mods = newMods;
            }
            catch (Exception e)
            {
                LogFile.WriteLine("An error occured while loading client mods: " + e);
                throw;
            }
        }
    }
}
