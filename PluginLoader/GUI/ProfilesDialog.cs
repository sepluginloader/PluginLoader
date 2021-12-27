using System;
using System.Collections.Generic;
using System.Linq;
using avaness.PluginLoader.Data;

namespace avaness.PluginLoader.GUI
{
    public class ProfilesDialog : TableDialogBase
    {
        private static PluginConfig Config => Main.Instance.Config;
        private static Dictionary<string, Profile> ProfileMap => Config.ProfileMap;
        private static PluginList PluginList => Main.Instance.List;

        private readonly Action<Profile> onProfileLoaded;

        protected override string ItemName => "profile";
        protected override string[] ColumnHeaders => new[] { "Name", "Enabled plugins and mods" };
        protected override float[] ColumnWidths => new[] { 0.55f, 0.43f };

        public ProfilesDialog(string caption, Action<Profile> onProfileLoaded) : base(caption)
        {
            this.onProfileLoaded = onProfileLoaded;
        }

        protected override IEnumerable<string> IterItemKeys() => ProfileMap.Keys.ToArray();

        protected override ItemView GetItemView(string key)
        {
            if (!ProfileMap.TryGetValue(key, out var profile))
                return null;

            var locals = 0;
            var plugins = 0;
            var mods = 0;
            foreach (var id in profile.Plugins)
            {
                if (!PluginList.TryGetPlugin(id, out var plugin))
                    continue;

                switch (plugin)
                {
                    case ModPlugin:
                        mods++;
                        break;

                    case LocalPlugin:
                        locals++;
                        break;

                    default:
                        plugins++;
                        break;
                }
            }

            var infoItems = new List<string>();
            if (locals > 0)
                infoItems.Add(locals > 1 ? $"{locals} local plugins" : "1 local plugin");
            if (plugins > 0)
                infoItems.Add(plugins > 1 ? $"{plugins} plugins" : "1 plugin");
            if (mods > 0)
                infoItems.Add(mods > 1 ? $"{mods} mods" : "1 mod");

            var info = string.Join(", ", infoItems);
            var labels = new[] { profile.Name, info };

            var total = locals + plugins + mods;
            var values = new object[] { null, total };

            return new ItemView(labels, values);
        }

        protected override object[] ExampleValues => new object[] { null, 0 };

        protected override void OnLoad(string key)
        {
            if (!ProfileMap.TryGetValue(key, out var profile))
                return;

            onProfileLoaded(profile);
        }

        protected override void OnRenamed(string key, string name)
        {
            if (!ProfileMap.TryGetValue(key, out var profile))
                return;

            profile.Name = name;
        }

        protected override void OnDelete(string key)
        {
            ProfileMap.Remove(key);
            Config.Save();
        }
    }
}