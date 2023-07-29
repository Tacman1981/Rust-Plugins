using System;
using System.Reflection;
using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Fast Food", "Verkade/Maintained by Tacman", "0.0.3")]
    [Description("No delay between eating")]
    public class FastFood : CovalencePlugin
    {
        private PluginConfig config;

        private void Init()
        {
            LoadConfig();
            Puts("Fast Food makes you fat!");
        }

        private void LoadConfig()
        {
            config = Config.ReadObject<PluginConfig>();

            if (config == null)
            {
                config = new PluginConfig();
                SaveConfig();
            }
        }

        private void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultConfig()
        {
            config = new PluginConfig();
            config.BlacklistedItems.Add("largemedkit");
            SaveConfig();
        }

        private void OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player.metabolism == null || IsBlacklisted(item.info.shortname))
                return;

            FieldInfo lastConsumeTimeField = player.metabolism.GetType().GetField("lastConsumeTime", BindingFlags.Instance | BindingFlags.NonPublic);
            if (lastConsumeTimeField != null)
            {
                lastConsumeTimeField.SetValue(player.metabolism, float.NegativeInfinity);
            }
        }

        private bool IsBlacklisted(string shortname)
        {
            return config.BlacklistedItems.Contains(shortname);
        }

        private class PluginConfig
        {
            public List<string> BlacklistedItems { get; set; }

            public PluginConfig()
            {
                BlacklistedItems = new List<string> { "largemedkit" };
            }
        }
    }
}
