//████████╗ █████╗  ██████╗███╗   ███╗  █████╗ ███╗   ██╗
//╚══██╔══╝██╔══██╗██╔════╝████╗ ████║ ██╔══██╗████╗  ██║
//   ██║   ███████║██║     ██╔████╔██║ ███████║██╔██╗ ██║
//   ██║   ██╔══██║██║     ██║╚██╔╝██║ ██╔══██║██║╚██╗██║
//   ██║   ██║  ██║╚██████╗██║ ╚═╝ ██║ ██║  ██║██║ ╚████║
//   ╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝     ╚═╝ ╚═╝  ╚═╝╚═╝  ╚═══╝
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Learn BPs Command", "Tacman", "1.0.0")]
    [Description("Adds a command to learn all blueprints with a scrap cost.")]
    class LearnBPs : RustPlugin
    {
        private int scrapCost = 1; // Adjusted scrap cost
        private const string permissionName = "learnbps.use"; // Permission name

        void Init()
        {
            // Register permission
            permission.RegisterPermission(permissionName, this);

            // Register command
            AddCommand("bps", "cmdLearnBPs");
        }

        [ChatCommand("bps")]
        private void cmdLearnBPs(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permissionName))
            {
                SendReply(player, "unknown command: bps");
                return;
            }

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

        private void AddCommand(string command, string callback) =>
            cmd.AddChatCommand(command, this, callback);
    }
}
