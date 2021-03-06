using HarmonyLib;
using Sandbox.Game.World;

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
