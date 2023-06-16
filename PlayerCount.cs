using Oxide.Core;
using Rust;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Player Count", "Tacman", "1.0.0")]
    public class PlayerCount : CovalencePlugin
    {
        private Dictionary<string, float> playerLastCommandTimes = new Dictionary<string, float>();
        private PluginConfig config;

        private class PluginConfig
        {
            public int DelaySeconds { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                DelaySeconds = 10 // Default delay of 10 seconds
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<PluginConfig>() ?? GetDefaultConfig();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Online Players: {0}" },
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Joueurs en ligne : {0}" },
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Online-Spieler: {0}" },
            }, this, "de");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Jugadores en línea: {0}" },
            }, this, "es");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Giocatori online: {0}" },
            }, this, "it");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] Онлайн игроки: {0}" },
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] 在线玩家数：{0}" },
            }, this, "zh-CN");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "[Player Count] اللاعبين المتصلين: {0}" },
            }, this, "ar");
        }

        private string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        private bool CanExecuteCommand(IPlayer player)
        {
            float lastCommandTime;
            if (playerLastCommandTimes.TryGetValue(player.Id, out lastCommandTime))
            {
                float currentTime = Time.realtimeSinceStartup;
                int commandDelay = config.DelaySeconds;

                if (currentTime - lastCommandTime < commandDelay)
                player.Reply("You can not use this command more than once every 60 seconds");
                    return false;
            }

            playerLastCommandTimes[player.Id] = Time.realtimeSinceStartup;
            return true;
        }

        [Command("pop")]
        private void PopCommand(IPlayer player, string command, string[] args)
        {
            if (!CanExecuteCommand(player))
                return;

            int playerCount = BasePlayer.activePlayerList.Count;

            string playerCountText = string.Format(GetMessage("PlayerCountFormat"), playerCount);

            player.Message(playerCountText);

            foreach (var onlinePlayer in players.Connected)
            {
                if (onlinePlayer.Id != player.Id)
                    onlinePlayer.Message(playerCountText);
            }
        }
    }
}
