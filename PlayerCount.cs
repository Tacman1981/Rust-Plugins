using Oxide.Core;
using Rust;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Player Count", "Tacman", "1.0.0")]
    public class PlayerCount : CovalencePlugin
    {
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

        [Command("pop")]
        private void PopCommand(IPlayer player, string command, string[] args)
        {
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
