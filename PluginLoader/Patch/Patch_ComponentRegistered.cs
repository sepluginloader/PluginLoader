using HarmonyLib;
using Sandbox.Game.World;
using VRage.Game.Components;
using VRage.Utils;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MySession), "RegisterComponentsFromAssemblies")]
    public static class Patch_ComponentRegistered
    {
        public static void Postfix()
        {
            Main.Instance?.RegisterComponents();
        }
    }
}
