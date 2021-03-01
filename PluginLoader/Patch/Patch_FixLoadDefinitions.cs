using HarmonyLib;
using System.IO;
using VRage.FileSystem;
using VRage.Game;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyModContext), "ModPathData", MethodType.Setter)]
    public static class Patch_FixLoadDefinitions
    {
        public static void Postfix(string value)
        {
            // This fixes a keen bug where if the Data directory doesn't exist, it displays multiple completely unnecessary errors on the screen.
            if (value != null && !Directory.Exists(value))
                MyFileSystem.CreateDirectoryRecursive(value);
        }
    }
}
