using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Rust;
using UnityEngine;
using Oxide.Game.Rust.Cui;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;

namespace Oxide.Plugins
{
    [Info("Purge", "Tacman", "0.0.4")]
    [Description("A plugin to unload specified plugins")]
    public class Purge : CovalencePlugin
    {
        private const string PurgePermission = "purge.use";
        private const int CooldownSeconds = 3000000;
        private readonly Dictionary<string, DateTime> _commandCooldowns = new Dictionary<string, DateTime>();
        private List<string> _pluginsToUnload;
        private bool _isPurgeActive = false;

        private void Init()
        {
            permission.RegisterPermission(PurgePermission, this);
            LoadConfigValues();
        }

        protected override void LoadDefaultConfig()
        {
            Config["PluginsToUnload"] = new List<string>
            {
                "TruePVE",
                "LootDefender",
                "PreventLooting"
            };
        }

        private void LoadConfigValues()
        {
            _pluginsToUnload = Config.Get<List<string>>("PluginsToUnload");
        }

        [Command("purge")]
        private void PurgeCommand(IPlayer player, string command, string[] args)
        {
            if (!player.HasPermission(PurgePermission))
            {
                player.Message("You don't have permission to use this command.");
                return;
            }

            DateTime currentTime = DateTime.UtcNow;
            if (_commandCooldowns.TryGetValue(player.Id, out DateTime lastUsage))
            {
                double remainingCooldown = CooldownSeconds - (currentTime - lastUsage).TotalSeconds;
                if (remainingCooldown > 0)
                {
                    player.Message($"Command is on cooldown. Please wait {Math.Ceiling(remainingCooldown)} seconds before using it again.");
                    return;
                }
            }

            if (_pluginsToUnload == null)
            {
                Puts("Plugins to unload field is empty, nothing has been unloaded");
                player.Reply("There are no plugins set to unload, is this intentional?");
                return;
            }

            foreach (string pluginName in _pluginsToUnload)
            {
                string consoleCmd = $"o.unload {pluginName}";
                ConsoleSystem.Run(ConsoleSystem.Option.Server, consoleCmd);
            }

            player.Message("Unloading specified plugins...");
            server.Broadcast("Purge has begun, PVP is now active!");

            ShowPurgeUI();
            _isPurgeActive = true;
            _commandCooldowns[player.Id] = currentTime;
        }

        private void ShowPurgeUI()
        {
            // Create a container for the UI elements
            var elements = new CuiElementContainer();

            // Create a completely non-blocking button for the panel
            var panel = new CuiButton
            {
                Button =
                {
                    Command = string.Empty,  // No commands executed
                    Color = "0 0 0 0",       // Fully transparent button background
                },
                RectTransform =
                {
                    AnchorMin = "0.45 0.85", // Smaller panel placed near the top-middle
                    AnchorMax = "0.55 0.95"
                },
                Text = { Text = string.Empty } // No visible text on the button
            };

            // Add the panel to the container (as a "non-blocking" base element)
            elements.Add(panel, "Hud", "PurgePanel");

            // Add a visible label for the notification text
            var label = new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "Purge Active!! PVP Enabled!!!",  // Notification text
                    FontSize = 12,  // Font size
                    Align = TextAnchor.MiddleCenter,  // Center-align text
                    Color = "1 0 0 1"  // Red text
                }
            };

            // Add the label to the "PurgePanel"
            elements.Add(label, "PurgePanel");

            // Add the UI to all active players
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.AddUi(player, elements);
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player != null && _isPurgeActive)
            {
                ShowPurgeUI();
            }
        }
        // Optional: Clean up the UI when the purge ends or when players disconnect
        private void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PurgePanel");
        }

        private void Unload()
        {
            // This will clean up the UI for all active players
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "PurgePanel");
                _isPurgeActive = false;
            }
        }
    }
}
