using HarmonyLib;
using Sandbox.Game;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens;
using Sandbox.Game.Screens.Helpers;
using SEPluginManager;
using SpaceEngineers.Game;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using VRageMath;

namespace avaness.PluginLoader
{
    public static class LoaderTools
    {
        public static Form GetMainForm()
        {
            if (Application.OpenForms.Count > 0)
                return Application.OpenForms[0];
            else
                return new Form { TopMost = true };
        }

        public static void Restart()
        {
            Application.Restart();
            Process.GetCurrentProcess().Kill();
        }

        public static bool UnpatchAll(Harmony harmony, Assembly patchOwner)
        {
            bool result = false;
            foreach (MethodBase patched in Harmony.GetAllPatchedMethods())
            {
                Patches patches = Harmony.GetPatchInfo(patched);
                if (patches != null)
                {
                    if (UnpatchAll(harmony, patched, patches.Prefixes, patchOwner))
                        result = true;
                    if (UnpatchAll(harmony, patched, patches.Postfixes, patchOwner))
                        result = true;
                    if (UnpatchAll(harmony, patched, patches.Transpilers, patchOwner))
                        result = true;
                    if (UnpatchAll(harmony, patched, patches.Finalizers, patchOwner))
                        result = true;
                }
            }
            return result;
        }

        public static bool UnpatchAll(Harmony harmony, MethodBase original, IEnumerable<HarmonyLib.Patch> patches, Assembly patchOwner)
        {
            bool result = false;
            foreach (HarmonyLib.Patch p in patches)
            {
                MethodInfo method = p.PatchMethod;
                if (method.DeclaringType.Assembly == patchOwner)
                {
                    harmony.Unpatch(original, method);
                    result = true;
                }
            }
            return result;
        }

        public static void ExecuteMain(LogFile log, SEPMPlugin plugin)
        {
            try
            {
                string name = plugin.GetType().ToString();
                log.WriteLine("Executing Main of " + name);
                plugin.Main(new Harmony(name), new Logger(name, log));
            }
            catch (Exception e)
            {
                log.WriteLine("Error while calling SEPM Main: " + e);
            }
        }

        public static string GetHash(string file)
        {
            using (FileStream fileStream = new FileStream(file, FileMode.Open))
            {
                using (BufferedStream bufferedStream = new BufferedStream(fileStream))
                {
                    using (SHA1Managed sha = new SHA1Managed())
                    {
                        byte[] hash = sha.ComputeHash(bufferedStream);
                        StringBuilder sb = new StringBuilder(2 * hash.Length);
                        foreach (byte b in hash)
                            sb.AppendFormat("{0:x2}", b);
                        return sb.ToString();
                    }
                }
            }
        }

        public static void ResetGameSettings()
        {
            MyGUISettings gui = new MyGUISettings()
            {
                EnableTerminalScreen = true,
                EnableToolbarConfigScreen = true,
                MultipleSpinningWheels = true,
                LoadingScreenIndexRange = new Vector2I(1, 16),
                HUDScreen = typeof(MyGuiScreenHudSpace),
                ToolbarConfigScreen = typeof(MyGuiScreenCubeBuilder),
                ToolbarControl = typeof(MyGuiControlToolbar),
                EditWorldSettingsScreen = typeof(MyGuiScreenWorldSettings),
                HelpScreen = typeof(MyGuiScreenHelpSpace),
                VoxelMapEditingScreen = MyPerGameSettings.GUI.VoxelMapEditingScreen, // This type is not public
                AdminMenuScreen = typeof(MyGuiScreenAdminMenu),
                CreateFactionScreen = typeof(MyGuiScreenCreateOrEditFaction),
                PlayersScreen = typeof(MyGuiScreenPlayers)
            };
            MyPerGameSettings.GUI = gui;
            SpaceEngineersGame.SetupPerGameSettings();
        }
    }
}
