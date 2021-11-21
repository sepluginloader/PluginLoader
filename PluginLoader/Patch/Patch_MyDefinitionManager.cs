using HarmonyLib;
using Sandbox.Game.World;
using System;
using System.Windows.Forms;
using VRage.Game;
using System.Linq;
using System.Reflection;
using VRage.Utils;
using avaness.PluginLoader.Data;
using System.Collections.Generic;
using System.IO;
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
                foreach (string id in Main.Instance.Config.Plugins)
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
