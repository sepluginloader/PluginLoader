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
    public partial class WorkshopPlugin : PluginData
    {
        public override string Source => MyTexts.GetString(MyCommonTexts.Workshop);
        public override string FriendlyName { get; }

        private readonly string assembly;

        protected WorkshopPlugin()
        {

        }

        public WorkshopPlugin(ulong id, string pluginFile) : base(id.ToString())
		{
            string name = Path.GetFileNameWithoutExtension(pluginFile).Replace('_', ' ');
            if (string.IsNullOrWhiteSpace(name))
                FriendlyName = Id;
            else
                FriendlyName = name;
            assembly = pluginFile;
        }

        public override string GetDllFile()
        {
            return assembly;

        }
    }
}
