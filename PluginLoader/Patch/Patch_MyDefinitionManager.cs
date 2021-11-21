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
using VRage.GameServices;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyDefinitionManager), "LoadData")]
    public static class Patch_MyDefinitionManager
    {
        internal static (MySession, List<MyWorkshopItem>)? ModsCache;

        public static void Prefix(ref List<MyObjectBuilder_Checkpoint.ModItem> mods)
        {
            if (ModsCache.HasValue && ModsCache.Value.Item1 == MySession.Static)
                mods.AddRange(ModsCache.Value.Item2.Select(item =>
                {
                    MyObjectBuilder_Checkpoint.ModItem modItem = new MyObjectBuilder_Checkpoint.ModItem(item.Id, item.ServiceName);
                    modItem.SetModData(item);
                    return modItem;
                }));
            ModsCache = null;
        }
    }
}
