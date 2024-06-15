using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using System;

namespace Oxide.Plugins
{
    [Info("Tool Cupboard Limit", "Tacman", "1.1.1")]
    class TCLimiter : RustPlugin
    {
        private Dictionary<ulong, int> placedCupboards = new Dictionary<ulong, int>();

        void OnServerInitialized()
        {
            LoadExistingCupboards();
        }

        void OnEntityBuilt(Planner planner, GameObject go)
        {
            var player = planner?.GetOwnerPlayer();
            if (player == null || go == null)
                return;

            var buildingPrivilege = go.GetComponent<BuildingPrivlidge>();
            if (buildingPrivilege == null || buildingPrivilege.OwnerID != player.userID)
                return;

            var maxCupboards = GetMaxCupboards(player);

            if (!placedCupboards.TryGetValue(player.userID, out int count))
            {
                placedCupboards[player.userID] = 0; // Initialize count if not found
            }

            if (count >= maxCupboards)
            {
                player.ChatMessage("You have reached your maximum allowed tool cupboards.");
                buildingPrivilege.Kill();
                return;
            }

            placedCupboards[player.userID]++;
            player.ChatMessage($"You have placed {placedCupboards[player.userID]} of {maxCupboards} tool cupboards.");
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            var cupboard = entity.GetComponent<BuildingPrivlidge>();
            if (cupboard == null || cupboard.OwnerID == 0)
                return;
        
            if (placedCupboards.ContainsKey(cupboard.OwnerID) && placedCupboards[cupboard.OwnerID] > 0)
            {
                placedCupboards[cupboard.OwnerID]--;
            }
        
            // Schedule LoadExistingCupboards to run on the next server tick
            NextTick(() =>
            {
                LoadExistingCupboards();
            });
        }

        [ChatCommand("TC")]
        void TCCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            LoadExistingCupboards();

            if (args.Length == 0)
            {
                if (placedCupboards.TryGetValue(player.userID, out int count))
                {
                    var maxCupboards = GetMaxCupboards(player);
                    player.ChatMessage($"You have placed {count} out of {maxCupboards} tool cupboards.");
                    player.ChatMessage($"You have {maxCupboards - count} tool cupboards left.");
                }
                else
                {
                    player.ChatMessage("You have placed no Tool Cupboards yet!");
                }
            }
            else
            {
                player.ChatMessage("Usage: /TC");
            }
        }

        void LoadExistingCupboards()
        {
            placedCupboards.Clear();

            foreach (var entity in BaseNetworkable.serverEntities)
            {
                var cupboard = entity.GetComponent<BuildingPrivlidge>();
                if (cupboard != null && cupboard.OwnerID != 0)
                {
                    if (!placedCupboards.ContainsKey(cupboard.OwnerID))
                        placedCupboards[cupboard.OwnerID] = 0;

                    placedCupboards[cupboard.OwnerID]++;
                }
            }
        }

        int GetMaxCupboards(BasePlayer player)
        {
            var permissions = Config.Get<Dictionary<string, object>>("Permissions");
            foreach (var kvp in permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    return Convert.ToInt32(kvp.Value);
                }
            }
            return Config.Get<int>("MaxCupboards"); // Default max cupboards if no permissions match
        }

        protected override void LoadDefaultConfig()
        {
            Config["MaxCupboards"] = 1; // Default value as integer
            var defaultPermissions = new Dictionary<string, object>
            {
                { "tclimiter.vip", 10 },
                { "tclimiter.discord", 8 },
                { "tclimiter.bypass", 100 }
            };
            Config["Permissions"] = defaultPermissions;
            SaveConfig();
        }
    }
}
