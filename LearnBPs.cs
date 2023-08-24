using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Learn BPs Command", "Tacman", "1.0.0")]
    [Description("Adds a command to learn all blueprints with a scrap cost.")]
    class LearnBPs : RustPlugin
    {
        private int scrapCost = 20745; // Adjusted scrap cost

        [ChatCommand("learnbps")]
        private void cmdLearnBPs(BasePlayer player, string command, string[] args)
        {
            int currentScrap = player.inventory.GetAmount(-932201673); // Current scrap in inventory

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
