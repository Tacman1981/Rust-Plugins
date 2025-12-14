using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

//Bunch of optimizations and some minor changes for version 1.1.1
//Bunch more optimzation for 1.2.1
//1.2.2 Checks for ores and furnaces now use HashSet for faster lookups
//Might add more configurable settings in the future. Potentially the list of BaseOvens affected and the cookable types.

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.3.0")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline if permission 'onoff' granted")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private string usePerm = "furnacenoorestop.use";
        private Dictionary<ulong, List<BaseOven>> cookers = new(); //This is to keep track of player ovens, if config setting for on and off are enabled. It also keeps track for turning off when no more ores or crude.
        /*private static readonly HashSet<string> checkOvens = new() //to keep track of all valid furnace and refinery prefab names
        {
            "furnace",
            "furnace.small",
            "furnace.large",
            "legacy_furnace",
            "electricfurnace.deployed",
            "refinery_small_deployed",
            "refinery"
        };*/
        private static readonly HashSet<string> checkOres = new()
        {
            "metal.ore",
            "hq.metal.ore",
            "sulfur.ore",
            "crude.oil"
        };
        private static readonly HashSet<uint> prefabID = new()
        {
            3808299817, //electric furnace deployed
            2931042549, //regular furnace deployed
            2013224025, //legacy furnace deployed
            1374462671, //large furnace deployed
            1057236622 //oil refinery deployed
        };
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

            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>()) //Only doing this once at server start to populate the dictionary, i prefer this over saving data, it is much more accurate this way.
            {
                if (oven.OwnerID == 0) continue;
                if (!prefabID.Contains(oven.prefabID)) continue; //only care about furnaces and refineries, checks prefab id now

                if (!cookers.TryGetValue(oven.OwnerID, out var list))
                    cookers[oven.OwnerID] = list = new List<BaseOven>();

                if (!list.Contains(oven))
                    list.Add(oven);
            }
        }

        void OnEntitySpawned(BaseOven oven) //decided to use BaseOven here instead of BaseNetworkable for optimization
        {
            if (oven == null) return;
            if (oven.OwnerID == 0) return;
            if (!prefabID.Contains(oven.prefabID)) return;

            if (!cookers.TryGetValue(oven.OwnerID, out var list))
                cookers[oven.OwnerID] = list = new List<BaseOven>();

            if (!list.Contains(oven))
                list.Add(oven);
            if (config.debug)
                Puts($"Added {oven.ShortPrefabName} for owner {BasePlayer.FindByID(oven.OwnerID)?.displayName}");
        }

        void OnEntityKill(BaseOven oven) //same again with BaseOven instead of BaseNetworkable
        {
            if (oven == null) return;
            if (!cookers.TryGetValue(oven.OwnerID, out var list)) return;

            list.Remove(oven);
            if (list.Count == 0)
                cookers.Remove(oven.OwnerID);
        }
        //hashset lookups should be more performant
        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null) return;

            if (!prefabID.Contains(oven.prefabID)) return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            bool hasOres = false;
            foreach (var i in oven.inventory.itemList)
            {
                if (checkOres.Contains(i.info.shortname))
                {
                    hasOres = true;
                    break;
                }
            }

            if (hasOres)
            {
                if (!oven.IsOn())
                {
                    oven.StartCooking();
                    if (config.debug)
                        Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as ore/crude was added.");
                }
            }
            else
            {
                oven.StopCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned off as it has no more ores/crude inside.");
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
            if (!checkOres.Contains(item.info.shortname)) return;
            if (!prefabID.Contains(oven.prefabID)) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if (!oven.IsOn())
            {
                oven.StartCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as ore/crude was added.");
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BaseOven oven)
        {
            if (oven == null) return;
            if (!prefabID.Contains(oven.prefabID)) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if (!oven.IsOn())
            {
                oven.StartCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as it has ores/crude inside.");
            }
            if(config.debug)
                Puts($"{player.displayName} looted furnace with prefab id {oven.prefabID}");
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
        //removed the LINQ check here to increase performance when players log in and out. Now uses a direct dictionary lookup.
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
