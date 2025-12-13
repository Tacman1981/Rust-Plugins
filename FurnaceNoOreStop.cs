using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

//Bunch of optimizations and some minor changes for version 1.1.1

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.2.0")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline if permission 'onoff' granted")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private string usePerm = "furnacenoorestop.use";
        public Dictionary<ulong, List<BaseOven>> cookers = new();

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
        void OnServerInitialized()
        {
            cookers.Clear();

            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (oven.OwnerID == 0) continue;
                if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) continue;

                if (!cookers.TryGetValue(oven.OwnerID, out var list))
                    cookers[oven.OwnerID] = list = new List<BaseOven>();

                if (!list.Contains(oven))
                    list.Add(oven);
            }
        }

        void OnEntitySpawned(BaseNetworkable ent)
        {
            if (ent is not BaseOven oven) return;
            if (oven.OwnerID == 0) return;
            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return;

            if (!cookers.TryGetValue(oven.OwnerID, out var list))
                cookers[oven.OwnerID] = list = new List<BaseOven>();

            if (!list.Contains(oven))
                list.Add(oven);
            if (config.debug)
                Puts($"Added {oven.ShortPrefabName} for owner {BasePlayer.FindByID(oven.OwnerID)?.displayName}");
        }

        void OnEntityKill(BaseNetworkable ent)
        {
            if (ent is not BaseOven oven) return;

            if (!cookers.TryGetValue(oven.OwnerID, out var list)) return;

            list.Remove(oven);
            if (list.Count == 0)
                cookers.Remove(oven.OwnerID);
        }

        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null) return;

            if (oven.ShortPrefabName != "furnace" && oven.ShortPrefabName != "legacy_furnace" && oven.ShortPrefabName != "electricfurnace.deployed" && oven.ShortPrefabName != "furnace.large" && oven.ShortPrefabName != "refinery_small_deployed")
                return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            var ores = new[] { "metal.ore", "hq.metal.ore", "sulfur.ore", "crude.oil" };
            bool hasOres = oven.inventory.itemList.Any(item => ores.Contains(item.info.shortname));

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
            if (!cookers.TryGetValue(player.userID, out var ovens)) return;

            foreach (BaseOven oven in ovens)
            {
                if (oven == null) continue;

                if (online && !oven.IsOn())
                {
                    oven.StartCooking();
                    if (config.debug)
                        Puts($"{oven.ShortPrefabName} owned by {player.displayName} turned on.");
                }
                else if (!online && oven.IsOn())
                {
                    oven.StopCooking();
                    if (config.debug)
                        Puts($"{oven.ShortPrefabName} owned by {player.displayName} turned off.");
                }
            }
        }

        #endregion
    }
} 
