using Oxide.Core;
using Rust;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Player Count", "Tacman", "1.2.0")]
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
                DelaySeconds = 60 // Default delay of 10 seconds
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
                { "PlayerCountFormat", "Online Players: {0}" },
                { "AdminCountFormat", "Online Admins: {0}" }
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Joueurs en ligne : {0}" },
                { "AdminCountFormat", "Administrateurs en ligne : {0}" }
            }, this, "fr");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Online-Spieler: {0}" },
                { "AdminCountFormat", "Online-Administratoren: {0}" }
            }, this, "de");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Jugadores en línea: {0}" },
                { "AdminCountFormat", "Administradores en línea: {0}" }
            }, this, "es");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Giocatori online: {0}" },
                { "AdminCountFormat", "Amministratori online: {0}" }
            }, this, "it");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Онлайн игроки: {0}" },
                { "AdminCountFormat", "Онлайн администраторы: {0}" }
            }, this, "ru");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "在线玩家数：{0}" },
                { "AdminCountFormat", "在线管理员数：{0}" }
            }, this, "zh-CN");

            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "اللاعبين المتصلين: {0}" },
                { "AdminCountFormat", "المسؤولين المتصلين: {0}" }
            }, this, "ar");

            // Add Dutch language support
            lang.RegisterMessages(new Dictionary<string, string>
            {
                { "PlayerCountFormat", "Aantal spelers online: {0}" },
                { "AdminCountFormat", "Aantal admins online: {0}" }
            }, this, "nl");
        }

        private string GetMessage(string key, string userId = null) => lang.GetMessage(key, this, userId);

        private bool CanExecuteCommand(IPlayer player)
        {
            float currentTime = Time.realtimeSinceStartup;
            float lastCommandTime;

            if (playerLastCommandTimes.TryGetValue(player.Id, out lastCommandTime))
            {
                float commandDelay = config.DelaySeconds;

                if (currentTime - lastCommandTime < commandDelay)
                    return false;
            }

            playerLastCommandTimes[player.Id] = currentTime;
            return true;
        }

        [Command("pop")]
        private void PopCommand(IPlayer player, string command, string[] args)
        {
            if (!CanExecuteCommand(player))
                return;

            int adminCount = 0;
            int playerCount = 0;

            foreach (var basePlayer in BasePlayer.activePlayerList)
            {
                if (basePlayer.IsAdmin)
                    adminCount++;
                else
                    playerCount++;
            }

            string playerCountText = string.Format(GetMessage("PlayerCountFormat", player.Id), playerCount);
            string adminCountText = string.Format(GetMessage("AdminCountFormat", player.Id), adminCount);

            string response = $"[Player Count] {playerCountText} - {adminCountText}";

            foreach (var onlinePlayer in players.Connected)
            {
                player.Reply(response, null, player.Id);
            }
        }
    }
}
