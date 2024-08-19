using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Command To Invite", "Tacman", "1.5.0")]
    [Description("Allows players to send team invites to other players")]
    class CommandToInvite : RustPlugin
    {
        private void SendInvite(BasePlayer sender, BasePlayer target)
        {
            RelationshipManager.PlayerTeam playerTeam = sender.Team;
            //Create a team if the sender is not already part of 1
            if (playerTeam == null)
            {
                if (!TryCreateTeam(sender))
                {
                    sender.ChatMessage("Failed to create team.");
                    return;
                }

                playerTeam = sender.Team;
            }
            //Check if team is full
            if (playerTeam.members.Count >= 8)
            {
                sender.ChatMessage("Your team is already full.");
                return;
            }
            //Check if sender is team leader
            if (!playerTeam.GetLeader().Equals(sender))
            {
                sender.ChatMessage("You are not the team leader.");
                return;
            }
            //Check for valid players
            if (target == null)
            {
                sender.ChatMessage("Player not found");
                return;
            }
            //Check if player is inviting self
            if (target == sender)
            {
                sender.ChatMessage("You can not invite yourself to team");
            }
            //Check if player is already in inviting team
            RelationshipManager.PlayerTeam targetTeam = target.Team;
            if (targetTeam != null && targetTeam == playerTeam)
            {
                sender.ChatMessage($"{target.displayName} is already in your team.");
                return;
            }
            //Check if player is already in another team
            if (targetTeam != null)
            {
                sender.ChatMessage($"{target.displayName} is already in a team.");
                return;
            }

            playerTeam.SendInvite(target);

            sender.ChatMessage($"You have invited {target.displayName} to your team.");
            target.ChatMessage($"{sender.displayName} has invited you to join their team from afar. Please press tab to accept or decline");
        }

        private bool TryCreateTeam(BasePlayer player)
        {
            if (global::RelationshipManager.maxTeamSize == 0)
            {
                player.ChatMessage("Teams are disabled on this server");
                return false;
            }
            if (player.currentTeam != 0UL) { return false; }
            RelationshipManager.PlayerTeam playerTeam = RelationshipManager.ServerInstance.CreateTeam();
            playerTeam.teamLeader = player.userID;
            playerTeam.AddPlayer(player);
            return true;
        }


        [ChatCommand("invite")]
        private void InviteCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /invite <player name or ID>");
                return;
            }
            string playerName = args[0];
            List<BasePlayer> players = BasePlayer.activePlayerList
                .Where(x => x.displayName.ToLower().Contains(playerName.ToLower()))
                .ToList();
            if (players.Count == 0) { player.ChatMessage("Player not found."); }
            else if (players.Count > 1)
            {
                var distinctPlayers = players.Select(x => x.displayName).Distinct().ToList();
                if (distinctPlayers.Count > 1) { player.ChatMessage($"Multiple players found. Please refine your search criteria."); return; }
                SendInvite(player, players.First());
            }
            else { SendInvite(player, players[0]); }
        }
    }
}
