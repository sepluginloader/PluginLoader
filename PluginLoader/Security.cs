using avaness.PluginLoader.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace avaness.PluginLoader
{
    internal static class Security
    {
        private static HashSet<string> whitelist = new HashSet<string>
        {
            "14baf624662e919ed15ef060c26a3d31596d339f7c5054213de71b9af45c6b3a",
            "0b5b7af0371d77a45dbdff983bd9b2638e0ec9047d8bf7cb56f6342e41898b18",
            "14be077e4bc7739882429d144605b5dfe47655b05675940da3ac95b8645569f7",
            "63a131b29abff4b2610acd45ea0f0194df91fcfac766dd342e365f6708129f4c",
            "39698c3dfe1150a48384af2c91a0b0ebeeac8d7a7aeeda8c19e46b11aea0dedf",
            "595e32d2360be12970bca12e163a651fa663e42048fc17e90ee3e6617282bd25",
        };

        public static bool Validate(LogFile log, string file)
        {
            string hash = LoaderTools.GetHash256(file);
            if (whitelist.Contains(hash))
                return true;
            log.WriteLine($"File '{file}' with hash {hash} is not on the whitelist!");
            return false;
        }
    }
}
