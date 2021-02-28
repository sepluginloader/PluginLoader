using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace avaness.PluginLoader.Patch
{
    [HarmonyPatch(typeof(MyGuiScreenLoading), "RunLoad")]
    public class Patch_InterruptLoading
    {
		public static void Postfix(MyGuiScreenLoading __instance)
		{
			MyGuiSandbox.CreateMessageBox(messageText: new StringBuilder("Test"), messageCaption: MyTexts.Get(MyCommonTexts.MessageBoxCaptionInfo));
		}
	}
}
