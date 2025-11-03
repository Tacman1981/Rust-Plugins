using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;

//Bunch of optimizations and some minor changes for version 1.1.1

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.1.1")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline if permission 'onoff' granted")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private string usePerm = "furnacenoorestop.use";

        #region Config
        static Configuration config;
        public class Configuration
        {
            [JsonProperty("Turn off furnaces when players disconnect?")]
            public bool disconnectOff;
            [JsonProperty("Turn on players furnaces when they connect?")]
            public bool connectOn;
            [JsonProperty("Debugging when player connect and disconnects?")]
            public bool debug;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    disconnectOff = false,
                    connectOn = false,
                    debug = false
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Setup

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
        }

        #endregion

        #region Hooks

        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null) return;

            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return; // I have changed this to be a whitelist instead of blacklist

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            bool hasOres = false; // Used to check if there are items inside the furnace or refinery
            foreach (Item item in oven.inventory.itemList)
            {
                if (item.info.shortname.Contains("ore") || item.info.shortname.Contains("crude"))
                {
                    hasOres = true; // Changing it true only if it has cookables inside
                    break;
                }
            }

            if (!hasOres)
            {
                oven.StopCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned off as it has no more ores/crude.");
            }
        }

        /*in here, it does not actually automatically turn on when the furnace already has existing items, so I use OnLootEntityEnd() to accomplish this.
         I have enabled this again so that industrial automation works properly as long as there are no ores or crude inside already.*/
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null)
                return;

            BaseOven oven = container.entityOwner as BaseOven;
            if (oven == null) return;

            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if ((item.info.shortname.Contains("crude") || item.info.shortname.Contains("ore")) && !oven.IsOn())
            {
                oven.StartCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as ore/crude was added.");
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BaseOven oven)
        {
            if (oven == null) return;
            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if (oven.inventory.itemList.Any(i => i.info.shortname.Contains("ore") || i.info.shortname.Contains("crude")) && !oven.IsOn())
            {
                oven.StartCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as it has ores/crude inside.");
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (config.connectOn)
                ToggleOvens(player, true);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (config.disconnectOff)
                ToggleOvens(player, false);
        }
        #endregion

        #region Helper

        private void ToggleOvens(BasePlayer player, bool online)
        {
            if (player == null) return;

            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (oven.OwnerID != player.userID) continue;

                if (online && !oven.IsOn())
                {
                    oven.StartCooking();
                    if (config.debug)
                        Puts($"Furnace owned by {player.displayName} has been turned on as they connected.");
                }
                else if (!online && oven.IsOn())
                {
                    oven.StopCooking();
                    if (config.debug)
                        Puts($"Furnace owned by {player.displayName} has been turned off as they disconnected.");
                }
            }
        }
        #endregion
    }
}
