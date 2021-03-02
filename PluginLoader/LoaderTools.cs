using HarmonyLib;
using SEPluginManager;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
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

        public static void Precompile(LogFile log, Assembly a)
        {
            log.WriteLine("Precompiling " + a);
            foreach (Type t in a.GetTypes())
            {
                if(!HasStaticConstructor(t))
                {
                    foreach (MethodInfo m in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        if (m.HasMethodBody() && !m.IsAbstract && !m.ContainsGenericParameters)
                            RuntimeHelpers.PrepareMethod(m.MethodHandle);
                    }
                }
            }
        }

        private static bool HasStaticConstructor(Type t)
        {
            return t.GetConstructors(BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance).Any(c => c.IsStatic);
        }
    }
}
