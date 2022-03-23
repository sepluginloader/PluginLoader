using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Text;
using VRage;
using VRage.Input;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyGuiScreenGamePlay), "ShowLoadMessageBox")]
    public static class Patch_IngameRestart
    {
        public static bool Prefix()
        {
            if(Main.Instance.HasLocal && MyInput.Static.IsAnyAltKeyPressed() && MyInput.Static.IsAnyCtrlKeyPressed())
            {
                var box = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.YES_NO, new StringBuilder("Plugin Loader: Are you sure you want to restart the game?"), MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm), callback: OnMessageClosed);
                box.SkipTransition = true;
                box.CloseBeforeCallback = true;
                MyGuiSandbox.AddScreen(box);
                return false;
            }
            return true;
        }

        private static void OnMessageClosed(MyGuiScreenMessageBox.ResultEnum result)
        {
            if(result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                LoaderTools.UnloadAndRestart();
            }
        }
    }
}
