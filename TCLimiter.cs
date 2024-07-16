using System.Collections.Generic;
using UnityEngine;
using Oxide.Core;
using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Tool Cupboard Limit", "Tacman", "1.1.1")]
    class TCLimiter : RustPlugin
    {
        private Dictionary<ulong, int> placedCupboards = new Dictionary<ulong, int>();

        void OnServerInitialized()
        {
            LoadDefaultConfig();
            LoadPermissions();
            LoadExistingCupboards();
        }

        void OnEntityBuilt(Planner planner, GameObject go)
        {
            var player = planner?.GetOwnerPlayer();
            if (player == null || go == null || player.IsAdmin)
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
                player.ChatMessage($"{player.displayName}, you have been fined 1 tool cupboard for attempting to breach cupboard limits.");
                buildingPrivilege.Kill();
                return;
            }

            placedCupboards[player.userID]++;
            player.ChatMessage($"You have placed {placedCupboards[player.userID]} of {maxCupboards} tool cupboards.");
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            var cupboard = entity?.GetComponent<BuildingPrivlidge>();
            if (cupboard == null || cupboard.OwnerID == 0)
                return;

            if (placedCupboards.ContainsKey(cupboard.OwnerID) && placedCupboards[cupboard.OwnerID] > 0)
            {
                LoadExistingCupboards();
            }
        }
        
        [ChatCommand("TC")]
        void TCCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null || player.IsAdmin)
            {
                player.ChatMessage("You are in admin mode and have no limits, enjoy");
                return;
            }
            
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
            // Clear the dictionary of placed cupboards
            placedCupboards.Clear();
        
            // Check if serverEntities is not null
            if (BaseNetworkable.serverEntities == null)
            {
                Debug.LogError("BaseNetworkable.serverEntities is null.");
                return;
            }
        
            // Iterate over each entity in the serverEntities
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                // Skip if the entity is null
                if (entity == null)
                {
                    continue;
                }
        
                // Try to get the BuildingPrivlidge component from the entity
                BuildingPrivlidge cupboard = entity.GetComponent<BuildingPrivlidge>();
        
                // If the cupboard component is found and has a valid OwnerID
                if (cupboard != null && cupboard.OwnerID != 0)
                {
                    // If the OwnerID is not already in the dictionary, add it with a count of 0
                    if (!placedCupboards.ContainsKey(cupboard.OwnerID))
                    {
                        placedCupboards[cupboard.OwnerID] = 0;
                    }
        
                    // Increment the count for the OwnerID
                    placedCupboards[cupboard.OwnerID]++;
                }
            }
        }

        int GetMaxCupboards(BasePlayer player)
        {
            var permissions = Config.Get<Dictionary<string, object>>("Permissions");
            int maxCupboards = Config.Get<int>("MaxCupboards"); // Default max cupboards
        
            foreach (var kvp in permissions)
            {
                if (permission.UserHasPermission(player.UserIDString, kvp.Key))
                {
                    int permissionMax = Convert.ToInt32(kvp.Value);
                    if (permissionMax > maxCupboards)
                    {
                        maxCupboards = permissionMax;
                    }
                }
            }
        
            return maxCupboards;
        }

        void LoadDefaultConfig()
        {
            // Get the current value of MaxCupboards from the config, convert it to int
            int defaultMaxCupboards;
            if (!int.TryParse(Config.Get<string>("MaxCupboards"), out defaultMaxCupboards))
            {
                defaultMaxCupboards = 1; // Set default value if MaxCupboards is not valid or found
            }
        
            // Set the default value in the config (if not already set)
            Config.Set("MaxCupboards", defaultMaxCupboards.ToString());
        
            // Set default permissions if not already set in the config
            var currentPermissions = Config.Get<Dictionary<string, object>>("Permissions");
            if (currentPermissions == null || currentPermissions.Count == 0)
            {
                var defaultPermissions = new Dictionary<string, object>
                {
                    { "tclimiter.vip", 10 },
                    { "tclimiter.discord", 8 },
                    { "tclimiter.bypass", 100 }
                };
        
                // Set default permissions in the config
                Config.Set("Permissions", defaultPermissions);
            }
        
            // Save the config to disk (if any changes were made)
            SaveConfig();
        }

        // Load permissions from the configuration
        void LoadPermissions()
        {
            var permissionsConfig = Config.Get<Dictionary<string, object>>("Permissions");
            foreach (var kvp in permissionsConfig)
            {
                permission.RegisterPermission(kvp.Key, this);
            }
        }
    }
}
