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

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyScriptManager), "LoadData")]
    public static class Patch_MyScripManager
    {
        private static Action<MyScriptManager, string, MyModContext> loadScripts;
        private static string workshopPath;

        static Patch_MyScripManager()
        {
            loadScripts = (Action<MyScriptManager, string, MyModContext>)Delegate.CreateDelegate(typeof(Action<MyScriptManager, string, MyModContext>), typeof(MyScriptManager).GetMethod("LoadScripts", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public static void Postfix(MyScriptManager __instance)
        {
            try
            {
                workshopPath = Path.GetFullPath(@"..\..\..\workshop\content\244850\");

                HashSet<ulong> currentMods;
                if (MySession.Static.Mods != null)
                    currentMods = new HashSet<ulong>(MySession.Static.Mods.Select(x => x.PublishedFileId));
                else
                    currentMods = new HashSet<ulong>();

                PluginList list = Main.Instance.List;
                foreach (string id in Main.Instance.Config.Plugins)
                {
                    PluginData data = list[id];
                    if (data is ModPlugin mod && !currentMods.Contains(mod.WorkshopId))
                        AddMod(__instance, mod.WorkshopId);
                }
            }
            catch (Exception e)
            {
                LogFile.WriteLine("An error occured while loading client mods: " + e);
                throw;
            }
        }

        private static void AddMod(MyScriptManager scriptManager, ulong modItemId)
        {
            string modLocation = Path.Combine(workshopPath, modItemId.ToString());
            if (!Directory.Exists(modLocation))
                return;

            MyModContext modContext = new MyModContext();
            modContext.Init(new MyObjectBuilder_Checkpoint.ModItem(modItemId, "Steam"));
            modContext.Init(modItemId.ToString(), null, modLocation);
            LogFile.WriteLine("Loading client mod " + modItemId);
            loadScripts(scriptManager, modLocation, modContext);
        }
    }
}
