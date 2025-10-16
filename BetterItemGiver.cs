using Oxide.Core;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Better Item Giver", "Tacman", "1.1.0")]
    [Description("Gives a specified item and amount to all players including sleepers.")]
    public class BetterItemGiver : RustPlugin
    {
        [ConsoleCommand("givesleepers")]
        private void GiveSleepersCmd(ConsoleSystem.Arg arg)
        {
            if (arg == null)
            {
                Puts("[BetterItemGiver] Invalid command context. Usage: givesleepers <shortname> <amount>");
                return;
            }

            BasePlayer player = arg.Player();
            if (player != null && !player.IsAdmin)
            {
                SendReply(player, "You are not authorized to use this command.");
                return;
            }
            if (player == null || player.IsAdmin)
            {
                {
                    var args = arg.Args ?? Array.Empty<string>();

                    if (args.Length < 2)
                    {
                        if (player != null)
                            Puts("Usage: givesleepers <shortname> <amount>");
                        else
                            Puts("[BetterItemGiver] Usage: givesleepers <shortname> <amount>");
                        return;
                    }

                    string shortname = args[0].ToLower();
                    if (!int.TryParse(args[1], out int amount) || amount <= 0)
                    {
                        if (player != null)
                            Puts("Invalid amount.");
                        else
                            Puts("[BetterItemGiver] Invalid amount.");
                        return;
                    }

                    var def = ItemManager.FindItemDefinition(shortname);
                    if (def == null)
                    {
                        if (player != null)
                            Puts($"Item '{shortname}' not found.");
                        else
                            Puts($"[BetterItemGiver] Item '{shortname}' not found.");
                        return;
                    }

                    int count = 0;
                    foreach (var target in BasePlayer.sleepingPlayerList)
                    {
                        if (target != null && target.IsSleeping())
                        {
                            GiveItemToPlayer(target, def, amount);
                            count++;
                        }
                    }

                    string msg = $"Gave {amount} x {def.displayName.english} to {count} sleepers.";
                    if (player != null)
                        Puts(msg);
                    else
                        Puts($"[BetterItemGiver] {msg}");
                }
            }
        }

        [ConsoleCommand("givetoall")]
        private void GiveAllCmd(ConsoleSystem.Arg arg)
        {
            if (arg == null)
            {
                Puts("[BetterItemGiver] Invalid command context. Usage: givesleepers <shortname> <amount>");
                return;
            }

            BasePlayer player = arg.Player();
            if (player != null && !player.IsAdmin)
            {
                SendReply(player, "You are not authorized to use this command.");
                return;
            }
            if (player == null || player.IsAdmin)
            {
                var args = arg.Args ?? Array.Empty<string>();

                if (args.Length < 2)
                {
                    if (player != null)
                        Puts("Usage: givesleepers <shortname> <amount>");
                    else
                        Puts("[BetterItemGiver] Usage: givesleepers <shortname> <amount>");
                    return;
                }

                string shortname = args[0].ToLower();
                if (!int.TryParse(args[1], out int amount) || amount <= 0)
                {
                    if (player != null)
                        Puts("Invalid amount.");
                    else
                        Puts("[BetterItemGiver] Invalid amount.");
                    return;
                }

                var def = ItemManager.FindItemDefinition(shortname);
                if (def == null)
                {
                    if (player != null)
                        Puts($"Item '{shortname}' not found.");
                    else
                        Puts($"[BetterItemGiver] Item '{shortname}' not found.");
                    return;
                }

                int count = 0;
                foreach (var target in BasePlayer.allPlayerList)
                {
                    if (target != null && target.IsSleeping())
                    {
                        GiveItemToPlayer(target, def, amount);
                        count++;
                    }
                }

                string msg = $"Gave {amount} x {def.displayName.english} to {count} sleepers.";
                if (player != null)
                    Puts(msg);
                else
                    Puts($"[BetterItemGiver] {msg}");
            }
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
