//Need to fix team leader being reset when kicking members

//████████╗ █████╗  ██████╗███╗   ███╗ █████╗ ███╗   ██╗
//╚══██╔══╝██╔══██╗██╔════╝████╗ ████║██╔══██╗████╗  ██║
//   ██║   ███████║██║     ██╔████╔██║███████║██╔██╗ ██║
//   ██║   ██╔══██║██║     ██║╚██╔╝██║██╔══██║██║╚██╗██║
//   ██║   ██║  ██║╚██████╗██║ ╚═╝ ██║██║  ██║██║ ╚████║
//   ╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝
using Oxide.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Team Handler", "Tacman", "1.0.0")]
    [Description("Handle all team management through commands")]
    class TeamHandler : RustPlugin
    {
        #region Helper Methods
        private void SendInvite(BasePlayer sender, BasePlayer target)
        {
            RelationshipManager.PlayerTeam playerTeam = sender.Team;
            if (playerTeam == null)
            {
                if (!TryCreateTeam(sender))
                {
                    sender.ChatMessage("Failed to create team.");
                    return;
                }

                playerTeam = sender.Team;
            }
            if (playerTeam.members.Count >= 8)
            {
                sender.ChatMessage("Your team is already full.");
                return;
            }
            if (!playerTeam.GetLeader().Equals(sender))
            {
                sender.ChatMessage("You are not the team leader.");
                return;
            }
            if (target == null)
            {
                sender.ChatMessage("Player not found");
                return;
            }
            if (target == sender)
            {
                sender.ChatMessage("You cannot invite yourself to the team");
            }
            RelationshipManager.PlayerTeam targetTeam = target.Team;
            if (targetTeam != null && targetTeam == playerTeam)
            {
                sender.ChatMessage($"{target.displayName} is already in your team.");
                return;
            }
            if (targetTeam != null)
            {
                sender.ChatMessage($"{target.displayName} is already in a team.");
                return;
            }

            playerTeam.SendInvite(target);

            sender.ChatMessage($"You have invited {target.displayName} to your team.");
            target.ChatMessage($"{sender.displayName} has invited you to their team. You can accept of decline from your inventory tab.");
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
        #endregion

        #region Commands
        [ChatCommand("invite")]
        private void InviteCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /invite <player name or ID>, team created if not already in 1");
                TryCreateTeam(player);
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
                if (distinctPlayers.Count > 1)
                {
                    player.ChatMessage($"Multiple players found. Please refine your search criteria.");
                    return;
                }
            }
            else
            {
                SendInvite(player, players[0]);
            }
        }

        [ChatCommand("leaveteam")]
        private void LeaveTeamCommand(BasePlayer player, string command, string[] args)
        {
            if (player.currentTeam == 0UL)
            {
                player.ChatMessage("You are not in a team.");
                return;
            }

            RelationshipManager.PlayerTeam playerTeam = player.Team;
            if (playerTeam != null && playerTeam.GetLeader() == player)
            {
                player.ChatMessage("You have left the team");
                playerTeam.RemovePlayer(player.userID);
                player.ClearTeam();
            }

            if (playerTeam != null && playerTeam.GetLeader() != player)
            {
                playerTeam.RemovePlayer(player.userID);
                player.ClearTeam();
                player.ChatMessage("You have left the team.");
            }
        }

        [ChatCommand("kick")]
        private void KickCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length != 1)
            {
                player.ChatMessage("Usage: /kick <player name or ID>");
                return;
            }

            ulong targetUserID = 0;

            if (!ulong.TryParse(args[0], out targetUserID))
            {
                List<BasePlayer> players = BasePlayer.activePlayerList
                    .Where(x => x.displayName.ToLower().Contains(args[0].ToLower()))
                    .ToList();

                if (players.Count == 0)
                {
                    player.ChatMessage("Player not found.");
                    return;
                }
                else if (players.Count > 1)
                {
                    var distinctPlayers = players.Select(x => x.displayName).Distinct().ToList();
                    player.ChatMessage($"Multiple players found. Please refine your search criteria.");
                    return;
                }
                targetUserID = players[0].userID;
            }

            var playerTeam = global::RelationshipManager.ServerInstance.FindTeam(player.currentTeam);
            if (playerTeam != null && playerTeam.GetLeader() == player)
            {
                if (player.userID == targetUserID)
                {
                    player.ChatMessage("You cannot kick yourself.");
                    return;
                }

                playerTeam.RemovePlayer(targetUserID);

                player.ChatMessage($"Player {player.displayName} has been kicked from your team.");
                BasePlayer targetPlayer = BasePlayer.Find(player.displayName);
                if (targetPlayer != null)
                {
                    targetPlayer.ClearTeam();
                    targetPlayer.ChatMessage("You have been kicked from the team.");
                }
            }
            else
            {
                player.ChatMessage("You must be the team leader to kick a player.");
            }
        }
        #endregion

        #region Hooks
        private object OnTeamCreate(BasePlayer player)
        {
            player.ChatMessage("You cannot create a team directly. Please use the /invite command to invite players to your team.");
            return true;
        }

        private object OnTeamInvite(BasePlayer inviter, BasePlayer target)
        {
            inviter.ChatMessage($"You can not invite the player this way, please use /invite {target.displayName}, partial names also work");
            return false;
        }

        private object OnTeamKick(RelationshipManager.PlayerTeam team, BasePlayer player, ulong targetUserID)
        {
            if (team.GetLeader() != player)
            {
                player.ChatMessage("You must be the team leader to kick players.");
                return false;
            }

            if (player.userID == targetUserID)
            {
                player.ChatMessage("You must use /leaveteam instead of trying to kick yourself.");
                return false;
            }

            player.ChatMessage("Please use the /kick command to kick players from your team.");
            return false;
        }

        private object OnTeamLeave(RelationshipManager.PlayerTeam team, BasePlayer player)
        {
            if (team.GetLeader() != player)
            {
                player.ChatMessage("To leave your team, use the /leaveteam command.");
                return true;
            }

            if (team.GetLeader() == player)
            {
                player.ChatMessage("Please use /leaveteam instead");
                return true;
            }

            return null;
        }

        void Init()
        {
            Puts("Manages teams with commands, no more accidental leaving by misclick or accidentally inviting someone you dont want in team. More features to come.");
        }
        #endregion
    }
}

