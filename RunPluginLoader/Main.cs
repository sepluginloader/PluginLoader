using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using VRage.FileSystem;
using VRage.Plugins;
using VRage.Utils;

namespace avaness.RunPluginLoader
{
    public class Main : IHandleInputPlugin
    {
        private readonly IHandleInputPlugin pluginLoader;
        private readonly Assembly harmony;

        public Main()
        {
            Log("Loading PluginLoader and dependencies...");
            try
            {
                DeleteExtraHarmony();

                string dir = GetAssemblyDirectory();
                harmony = Assembly.LoadFile(Path.Combine(dir, "0Harmony"));
                AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony;
                Assembly pluginLoaderAssembly = Assembly.LoadFile(Path.Combine(dir, "PluginLoader"));
                Type pluginType = typeof(IPlugin);
                Type pluginLoaderMain = pluginLoaderAssembly.GetTypes().Where(t => pluginType.IsAssignableFrom(t) && t.Name.Contains("Main")).FirstOrDefault();
                if (pluginLoaderMain != null)
                {
                    pluginLoader = (IHandleInputPlugin)Activator.CreateInstance(pluginLoaderMain);
                    Log($"PluginLoader started.");
                }
                else
                {
                    Log("Failed to find PluginLoader!");
                }
            }
            catch (ReflectionTypeLoadException re)
            {
                Log("Error: " + re);
                foreach (Exception e in re.LoaderExceptions)
                    Log(e.ToString());
                MessageBox.Show(GetMainForm(), "Plugin Loader crashed! Check game log for more info.");
                CloseCustomSplashScreen();
            }
            catch (Exception e)
            {
                Log("Error: " + e);
                MessageBox.Show(GetMainForm(), "Plugin Loader crashed: " + e);
                CloseCustomSplashScreen();
            }
        }

        private void CloseCustomSplashScreen()
        {
            foreach (Form f in Application.OpenForms)
            {
                if (f.Name == "SplashScreenPluginLoader")
                {
                    f.Close();
                    break;
                }
            }
        }

        private void DeleteExtraHarmony()
        {
            string dll = Path.Combine(MyFileSystem.ExePath, "0Harmony.dll");
            if(File.Exists(dll))
            {
                try
                {
                    File.Delete(dll);
                    Log("Deleted extra Harmony file.");
                }
                catch { }
            }
        }

        private void Log(string s)
        {
            MyLog.Default.WriteLine("[RunPluginLoader] " + s);
        }

        private Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            if(args.Name.Contains("0Harmony"))
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
                Log("0Harmony dependency loaded.");
                return harmony;
            }
            return null;
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveHarmony;
            pluginLoader?.Dispose();
        }

        public void Init(object gameInstance)
        {
            pluginLoader?.Init(gameInstance);
        }

        public void Update()
        {
            pluginLoader?.Update();
        }

        public void HandleInput()
        {
            pluginLoader?.HandleInput();
        }

        private static string GetAssemblyDirectory()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetFullPath(Path.GetDirectoryName(path));
        }

        private static Form GetMainForm()
        {
            if (Application.OpenForms.Count > 0)
                return Application.OpenForms[0];
            else
                return new Form { TopMost = true };
        }
    }
}
