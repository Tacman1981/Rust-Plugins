using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;

namespace Oxide.Plugins
{
    [Info("Learn BPs Command", "Tacman", "1.0.0")]
    [Description("Adds a command to learn all blueprints with a configurable scrap cost.")]

    class LearnBPs : RustPlugin
    {
        private int scrapCost;
        private string learnCommand;

        private int configVersion = 1; // Current configuration version

        protected override void LoadDefaultConfig()
        {
            Config["ConfigVersion"] = configVersion;
            Config["ScrapCost"] = 20745; // Default scrap cost
            Config["LearnCommand"] = "learnbps"; // Default command name
            SaveConfig();
        }

        private void Init()
        {
            LoadConfigData();
            CheckConfigVersion();
        }

        private void LoadConfigData()
        {
            scrapCost = Convert.ToInt32(Config["ScrapCost"]);
            learnCommand = Config["LearnCommand"].ToString().ToLower(); // Command name in lowercase
        }

        private void CheckConfigVersion()
        {
            int loadedConfigVersion = Convert.ToInt32(Config["ConfigVersion"]);

            if (loadedConfigVersion < configVersion)
            {
                // Handle configuration update logic here
                Puts("Updating configuration...");

                // Update configuration values

                Config["ConfigVersion"] = configVersion;
                SaveConfig();
            }
        }

        [ChatCommand("learnbps")]
        private void LearnBPsCommand(BasePlayer player, string command, string[] args)
        {
            int currentScrap = player.inventory.GetAmount(-932201673); // Current scrap in inventory

            if (command.ToLower() != learnCommand)
            {
                // Ignore the command if it doesn't match the configured command
                return;
            }

            if (currentScrap >= scrapCost)
            {
                player.inventory.Take(null, -932201673, scrapCost); // Deduct scrap from inventory
                player.blueprints.UnlockAll();
                SendReply(player, $"You have learned all blueprints by spending {scrapCost} scrap.");
            }
            else
            {
                int scrapShort = scrapCost - currentScrap;
                SendReply(player, $"You do not have enough scrap to learn all blueprints. You are short by {scrapShort} scrap.");
            }
        }
    }
}
