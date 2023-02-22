using HarmonyLib;
using Sandbox.Graphics.GUI;
using SpaceEngineers.Game.GUI;
using System.Text;
using avaness.PluginLoader.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

// ReSharper disable InconsistentNaming

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "CreateMainMenu")]
    public static class Patch_CreateMainMenu
    {
        public static void Postfix(MyGuiScreenMainMenu __instance, Vector2 leftButtonPositionOrigin, ref Vector2 lastButtonPosition)
        {
            MyGuiControlButton lastBtn = null;
            foreach (var control in __instance.Controls)
            {
                if (control is MyGuiControlButton btn && btn.Position == lastButtonPosition)
                {
                    lastBtn = btn;
                    break;
                }
            }

            Vector2 position;
            if (lastBtn == null)
            {
                position = lastButtonPosition + MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;
            }
            else
            {
                position = lastBtn.Position;
                lastBtn.Position = lastButtonPosition + MyGuiConstants.MENU_BUTTONS_POSITION_DELTA;
            }

            MyGuiControlButton openBtn = new MyGuiControlButton(position, MyGuiControlButtonStyleEnum.StripeLeft, originAlign: MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_BOTTOM, text: new StringBuilder("Plugins"), onButtonClick: _ => MainPluginMenu.Open())
            {
                BorderEnabled = false,
                BorderSize = 0,
                BorderHighlightEnabled = false,
                BorderColor = Vector4.Zero
            };
            __instance.Controls.Add(openBtn);
        }
    }


    [HarmonyPatch(typeof(MyGuiScreenMainMenu), "CreateInGameMenu")]
    public static class Patch_CreateInGameMenu
    {
        public static void Postfix(MyGuiScreenMainMenu __instance, Vector2 leftButtonPositionOrigin, ref Vector2 lastButtonPosition)
        {
            Patch_CreateMainMenu.Postfix(__instance, leftButtonPositionOrigin, ref lastButtonPosition);
        }
    }
}