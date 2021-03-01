using HarmonyLib;
using SEPluginManager;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

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

        public static void UnpatchAll(Harmony harmony, Assembly patchOwner)
        {
            foreach (MethodBase patched in Harmony.GetAllPatchedMethods())
            {
                Patches patches = Harmony.GetPatchInfo(patched);
                if (patches != null)
                {
                    UnpatchAll(harmony, patched, patches.Prefixes, patchOwner);
                    UnpatchAll(harmony, patched, patches.Postfixes, patchOwner);
                    UnpatchAll(harmony, patched, patches.Transpilers, patchOwner);
                    UnpatchAll(harmony, patched, patches.Finalizers, patchOwner);
                }
            }
        }

        public static void UnpatchAll(Harmony harmony, MethodBase original, IEnumerable<HarmonyLib.Patch> patches, Assembly patchOwner)
        {
            foreach (HarmonyLib.Patch p in patches)
            {
                MethodInfo method = p.PatchMethod;
                if (method.DeclaringType.Assembly == patchOwner)
                    harmony.Unpatch(original, method);
            }
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

        public static bool CheckAssemblyRef(AssemblyName name)
        {
            string s = name.FullName;
            if(s.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                if (s.StartsWith("System.Web", StringComparison.OrdinalIgnoreCase))
                    return false;
                else if (s.StartsWith("System.Net", StringComparison.OrdinalIgnoreCase))
                    return false;
                else if (s.StartsWith("System.Windows", StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }
    }
}
