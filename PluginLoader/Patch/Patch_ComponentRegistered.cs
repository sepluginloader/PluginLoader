using HarmonyLib;
using Sandbox.Game.World;
using System.Reflection;
using VRage.Game;
using VRage.Plugins;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MySession), "RegisterComponentsFromAssembly")]
    [HarmonyPatch(new[] { typeof(Assembly), typeof(bool), typeof(MyModContext) })]
    public static class Patch_ComponentRegistered
    {
        public static void Prefix(Assembly assembly)
        {
            if(assembly == MyPlugins.GameAssembly)
                Main.Instance?.RegisterComponents();
        }
    }
}