using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using HarmonyLib;
using VRage;
using VRage.Plugins;

namespace avaness.PluginLoader.Data
{
    public partial class WorkshopPlugin : SteamPlugin
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Workshop);
        protected override string HashFile => "hash.txt";

        private string assembly;

        protected WorkshopPlugin()
        {

        }

        public WorkshopPlugin(LogFile log, ulong id, string pluginFile) : base(log, id, pluginFile)
		{ }

        protected override void CheckForUpdates()
        {
            assembly = Path.Combine(root, Path.GetFileNameWithoutExtension(sourceFile) + ".dll");

            bool found = false;
            foreach (string dll in Directory.EnumerateFiles(root, "*.dll"))
            {
                if (dll == assembly)
                    found = true;
                else
                    File.Delete(dll);
            }
            if (!found)
                Status = PluginStatus.PendingUpdate;
            else
                base.CheckForUpdates();
        }

        protected override string GetName()
        {
            string name = Path.GetFileNameWithoutExtension(sourceFile).Replace('_', ' ');
            if (string.IsNullOrWhiteSpace(name))
                return Id;
            else
                return name;
        }

        protected override void ApplyUpdate()
        {
            File.Copy(sourceFile, assembly, true);
        }

        protected override string GetAssemblyFile()
        {
            return assembly;
        }
    }
}
