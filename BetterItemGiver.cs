using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Easier Item Giver", "Tacman", "1.0.0")]
    [Description("Gives a specified item and amount to all players including sleepers.")]
    public class BetterItemGiver : RustPlugin
    {
        [ConsoleCommand("givesleep")]
        private void GiveSleepersCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            string[] args = arg.Args;
            if (args == null)
                Puts("Command usage: giveitem {shortname} {amount}");

            if (args.Length < 2)
            {
                Puts("Usage: giveitem <shortname> <amount>");
                return;
            }

            string shortname = args[0].ToLower();
            if (!int.TryParse(args[1], out int amount) || amount <= 0)
            {
                Puts("Invalid amount.");
                return;
            }

            ItemDefinition def = ItemManager.FindItemDefinition(shortname);
            if (def == null)
            {
                Puts($"Item '{shortname}' not found.");
                return;
            }

            int count = 0;
            foreach (var target in BasePlayer.sleepingPlayerList)
            {
                if (target.IsSleeping())
                {
                    GiveItemToPlayer(target, def, amount);
                    count++;
                }
            }

            string msg = $"Gave {amount} x {shortname} to {count} sleepers.";
            Puts(msg);
        }

        [ConsoleCommand("givetoall")]
        private void GiveAllCmd(ConsoleSystem.Arg arg)
        {
            BasePlayer player = arg.Player();
            string[] args = arg.Args;
            if (args == null)
                Puts("Command usage: giveitem {shortname} {amount}");

            if (args.Length < 2)
            {
                Puts("Usage: giveitem <shortname> <amount>");
                return;
            }

            string shortname = args[0].ToLower();
            if (!int.TryParse(args[1], out int amount) || amount <= 0)
            {
                Puts("Invalid amount.");
                return;
            }

            ItemDefinition def = ItemManager.FindItemDefinition(shortname);
            if (def == null)
            {
                Puts($"Item '{shortname}' not found.");
                return;
            }

            int count = 0;
            foreach (var target in BasePlayer.allPlayerList)
            {
                GiveItemToPlayer(target, def, amount);
                count++;
            }

            string msg = $"Gave {amount} x {shortname} to {count} sleepers.";
            Puts(msg);
        }

        private void GiveItemToPlayer(BasePlayer target, ItemDefinition def, int amount)
        {
            Item item = ItemManager.Create(def, amount);
            if (!item.MoveToContainer(target.inventory.containerMain))
            {
                Puts($"Cannot give {def.displayName.english} to {target.displayName}");
                item.Drop(target.GetDropPosition(), target.GetDropVelocity());
            }
        }
    }
}
