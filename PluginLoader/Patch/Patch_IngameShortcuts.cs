using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Graphics.GUI;
using System.Diagnostics;
using System.IO;
using System.Text;
using VRage;
using VRage.Input;
using VRage.Utils;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyGuiScreenGamePlay), "HandleUnhandledInput")]
    public static class Patch_IngameShortcuts
    {
        public static bool Prefix()
        {
            IMyInput input = MyInput.Static;
            if (MySession.Static != null && input.IsAnyAltKeyPressed() && input.IsAnyCtrlKeyPressed())
            {
                if(input.IsNewKeyPressed(MyKeys.F5))
                {
                    ShowRestartMenu();
                    return false;
                }
                else if(input.IsNewKeyPressed(MyKeys.L))
                {
                    ShowLogMenu();
                    return false;
                }
            }
            return true;
        }

        public static void ShowLogMenu()
        {
            var box = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.YES_NO, new StringBuilder("Plugin Loader: Show game log?"), MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm), callback: OnLogMessageClosed);
            box.SkipTransition = true;
            box.CloseBeforeCallback = true;
            MyGuiSandbox.AddScreen(box);
        }

        private static void OnLogMessageClosed(MyGuiScreenMessageBox.ResultEnum @enum)
        {
            if (@enum != MyGuiScreenMessageBox.ResultEnum.YES)
                return;

            string file = MyLog.Default?.GetFilePath();
            if (File.Exists(file))
                Process.Start("explorer.exe", $"\"{file}\"");
        }

        public static void ShowRestartMenu()
        {
            var box = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Error, MyMessageBoxButtonsType.YES_NO, new StringBuilder("Plugin Loader: Are you sure you want to restart the game?"), MyTexts.Get(MyCommonTexts.MessageBoxCaptionPleaseConfirm), callback: OnRestartMessageClosed);
            box.SkipTransition = true;
            box.CloseBeforeCallback = true;
            MyGuiSandbox.AddScreen(box);
        }

        private static void OnRestartMessageClosed(MyGuiScreenMessageBox.ResultEnum result)
        {
            if(result == MyGuiScreenMessageBox.ResultEnum.YES)
            {
                LoaderTools.Unload();
                LoaderTools.Restart(true);
            }
        }
    }
}
