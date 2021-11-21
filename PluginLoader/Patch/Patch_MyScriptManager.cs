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
using VRage.GameServices;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyScriptManager), "LoadData")]
    public static class Patch_MyScripManager
    {
        private static Action<MyScriptManager, string, MyModContext> loadScripts;

        static Patch_MyScripManager()
        {
            loadScripts = (Action<MyScriptManager, string, MyModContext>)Delegate.CreateDelegate(typeof(Action<MyScriptManager, string, MyModContext>), typeof(MyScriptManager).GetMethod("LoadScripts", BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public static void Postfix(MyScriptManager __instance)
        {
            try
            {
                HashSet<ulong> currentMods;
                if (MySession.Static.Mods != null)
                    currentMods = new HashSet<ulong>(MySession.Static.Mods.Select(x => x.PublishedFileId));
                else
                    currentMods = new HashSet<ulong>();

                PluginList list = Main.Instance.List;
                List<MyWorkshopItem> items = SteamAPI.ResolveDependencies(Main.Instance.Config.Plugins.Select(id => list[id]).OfType<ModPlugin>().Select(mod => mod.WorkshopId));

                SteamAPI.Update(items.Select(item => item.Id));

                List<MyWorkshopItem> filteredItems = items.Where(item => !currentMods.Contains(item.Id)).ToList();
                foreach (MyWorkshopItem item in filteredItems)
                {
                    AddMod(__instance, item);
                }

                // Idk how "transfer" mods without calling resolve again
                Patch_MyDefinitionManager.ModsCache = (MySession.Static, filteredItems);
            }
            catch (Exception e)
            {
                LogFile.WriteLine("An error occured while loading client mods: " + e);
                throw;
            }
        }
        
        private static void AddMod(MyScriptManager scriptManager, MyWorkshopItem item)
        {
            string modLocation = Path.Combine(Path.GetFullPath(@"..\..\..\workshop\content\244850\"), item.Id.ToString());
            if (!Directory.Exists(modLocation))
                return;

            MyModContext modContext = new MyModContext();
            modContext.Init(new MyObjectBuilder_Checkpoint.ModItem(item.Id, item.ServiceName));
            modContext.Init(item.Id.ToString(), null, modLocation);
            LogFile.WriteLine("Loading client mod scripts " + item.Id);
            loadScripts(scriptManager, modLocation, modContext);
        }
    }
}
