using HarmonyLib;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using System;
using System.Text;
using VRage.Utils;
using VRageMath;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "RecreateControls")]
    public static class Patch_CreateMenu
    {
        public static void Postfix(MyGuiScreenMainMenu __instance)
        {
            __instance.AddControl(
                new MyGuiControlButton(MyGuiManager.ComputeFullscreenGuiCoordinate(MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, 0, 0), originAlign: MyGuiDrawAlignEnum.HORISONTAL_RIGHT_AND_VERTICAL_TOP, text: new StringBuilder("Plugins"), onButtonClick: OpenMenu, toolTip: "Click to configure plugins.")
                {
                    BorderEnabled = true,
                    BorderSize = 1,
                    BorderHighlightEnabled = true,
                    BorderColor = Vector4.Zero
                });
            
        }

        private static void OpenMenu(MyGuiControlButton btn)
        {
            MyGuiSandbox.AddScreen(new MyGuiScreenPluginConfig());
        }
    }
}
