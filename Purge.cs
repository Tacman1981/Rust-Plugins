using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries.Covalence;
using Rust;
using UnityEngine;
using Oxide.Game.Rust.Cui;

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

        protected override void LoadDefaultConfig() // Add more plugins here or add them through the actual config file. Case sensitive
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
            var elements = new CuiElementContainer();
        
            var panel = new CuiPanel
            {
                Image =
                {
                    Color = "0 0 0 0"
                },
                RectTransform =
                {
                    AnchorMin = "0.4 -0.5",
                    AnchorMax = "0.6 0.85"
                },
                CursorEnabled = false
            };
        
            string panelName = "PurgePanel";
        
            elements.Add(panel, "Hud", panelName);
        
            var label = new CuiLabel
            {
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                },
                Text =
                {
                    Text = "Purge Active!! PVP Enabled!!!",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter,
                    Color = "1 0 0 1"
                }
            };
        
            elements.Add(label, panelName);
        
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
        private void OnPlayerDisconnected(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PurgePanel");
        }
        
        private void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "PurgePanel");
                _isPurgeActive = false;
            }
        }
    }
}