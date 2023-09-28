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

    private class PluginConfig
    {
      public int DelaySeconds { get; set; }
    }

    private const string AdminCountPermission = "playercount.admin"; // New permission constant

    private PluginConfig config;

    protected void Init()
    {
      permission.RegisterPermission(AdminCountPermission, this);
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
      }, this, "en");

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
      string adminCountText = player.HasPermission(AdminCountPermission) ? string.Format(GetMessage("AdminCountFormat", player.Id), adminCount) : "";

      string response = $"[Player Count] {playerCountText} {adminCountText}";

      player.Reply(response, null, player.Id);
    }
  }
}
