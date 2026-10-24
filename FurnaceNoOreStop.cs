using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.3.1")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline if permission 'onoff' granted")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private string usePerm = "furnacenoorestop.use";
        private Dictionary<ulong, List<BaseOven>> cookers = new();

        private static readonly HashSet<string> checkOres = new()
        {
            "metal.ore",
            "hq.metal.ore",
            "sulfur.ore",
            "crude.oil"
        };

        private static readonly HashSet<uint> prefabID = new()
        {
            3808299817, // electric furnace deployed
            2931042549, // regular furnace deployed
            2013224025, // legacy furnace deployed
            1374462671, // large furnace deployed
            1057236622  // oil refinery deployed
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

            foreach (BaseNetworkable netEntity in BaseNetworkable.serverEntities)
            {
                BaseOven oven = netEntity as BaseOven;
                if (oven == null || oven.OwnerID == 0) continue;
                if (!prefabID.Contains(oven.prefabID)) continue;

                if (!cookers.TryGetValue(oven.OwnerID, out var list))
                    cookers[oven.OwnerID] = list = new List<BaseOven>();

                if (!list.Contains(oven))
                    list.Add(oven);
            }
        }

        void OnEntitySpawned(BaseOven oven)
        {
            if (oven == null || oven.OwnerID == 0) return;
            if (!prefabID.Contains(oven.prefabID)) return;

            if (!cookers.TryGetValue(oven.OwnerID, out var list))
                cookers[oven.OwnerID] = list = new List<BaseOven>();

            if (!list.Contains(oven))
                list.Add(oven);

            if (config.debug)
                Puts($"Added {oven.ShortPrefabName} for owner {BasePlayer.FindByID(oven.OwnerID)?.displayName}");
        }

        void OnEntityKill(BaseOven oven)
        {
            if (oven == null) return;
            if (!cookers.TryGetValue(oven.OwnerID, out var list)) return;

            list.Remove(oven);
            if (list.Count == 0)
                cookers.Remove(oven.OwnerID);
        }

        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null) return;
            if (!prefabID.Contains(oven.prefabID)) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            // Only inspect items if oven is currently cooking
            bool hasOres = false;
            var items = oven.inventory.itemList;
            for (int i = 0; i < items.Count; i++)
            {
                if (checkOres.Contains(items[i].info.shortname))
                {
                    hasOres = true;
                    break;
                }
            }

            // Only stop when ores/crude run out; no redundant work if still has ores
            if (!hasOres)
            {
                oven.StopCooking();
                if (config.debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned off as it has no more ores/crude inside.");
            }
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            // Immediate ultra-fast rejection for non-furnace containers
            if (container?.entityOwner is not BaseOven oven) return;

            // If oven is already running, no need to process
            if (oven.IsOn() || item == null) return;

            if (!prefabID.Contains(oven.prefabID)) return;
            if (!checkOres.Contains(item.info.shortname)) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            oven.StartCooking();
            if (config.debug)
                Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as ore/crude was added.");
        }

        private void OnLootEntityEnd(BasePlayer player, BaseOven oven)
        {
            if (oven == null || oven.IsOn() || oven.inventory == null) return;
            if (!prefabID.Contains(oven.prefabID)) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            // Verify the furnace actually has cookable ores before auto-starting
            bool hasOres = false;
            var items = oven.inventory.itemList;
            for (int i = 0; i < items.Count; i++)
            {
                if (checkOres.Contains(items[i].info.shortname))
                {
                    hasOres = true;
                    break;
                }
            }

            if (hasOres)
            {
                oven.StartCooking();
                if (config.debug)
                    Puts($"Furnace owned by {player.displayName} has been turned on as it has ores/crude inside.");
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

            // Iterate backwards to safely prune destroyed/null ovens from memory
            for (int i = ovens.Count - 1; i >= 0; i--)
            {
                BaseOven oven = ovens[i];
                if (oven == null || oven.IsDestroyed)
                {
                    ovens.RemoveAt(i);
                    continue;
                }

                if (online && !oven.IsOn())
                {
                    bool hasOres = false;
                    if (oven.inventory != null)
                    {
                        var items = oven.inventory.itemList;
                        for (int j = 0; j < items.Count; j++)
                        {
                            if (checkOres.Contains(items[j].info.shortname))
                            {
                                hasOres = true;
                                break;
                            }
                        }
                    }

                    if (hasOres)
                    {
                        oven.StartCooking();
                        if (config.debug)
                            Puts($"{oven.ShortPrefabName} owned by {player.displayName} turned on.");
                    }
                }
                else if (!online && oven.IsOn())
                {
                    oven.StopCooking();
                    if (config.debug)
                        Puts($"{oven.ShortPrefabName} owned by {player.displayName} turned off.");
                }
            }

            if (ovens.Count == 0)
            {
                cookers.Remove(player.userID);
            }
        }
        #endregion
    }
}
