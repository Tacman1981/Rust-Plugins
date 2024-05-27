using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Tool Cupboard Limit", "Tacman", "1.0.0")]
    class TCLimiter : RustPlugin
    {
        private Dictionary<ulong, int> placedCupboards = new Dictionary<ulong, int>();
        private int maxCupboards;

        void OnServerInitialized()
        {
            LoadConfigValues();
            DetectExistingCupboards();
            Debug.Log("TCLimiter: Plugin Initialized.");
        }

        private void DetectExistingCupboards()
        {
            // Iterate through all entities in the game world
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity == null) continue;
        
                // Check if the entity is a cupboard
                if (entity is BuildingPrivlidge && entity.ShortPrefabName.Contains("cupboard"))
                {
                    var buildingPrivlidge = entity as BuildingPrivlidge;
                    if (buildingPrivlidge != null)
                    {
                        var ownerID = buildingPrivlidge.OwnerID;
                        if (!placedCupboards.ContainsKey(ownerID))
                        {
                            placedCupboards[ownerID] = 1;
                        }
                        else
                        {
                            placedCupboards[ownerID]++;
                        }
                        //Debug.Log($"TCLimiter: Detected existing cupboard with owner ID {ownerID}.");
                    }
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            // Set default values only if the config doesn't exist
            if (Config.Get("MaxCupboards") == null)
            {
                Config.Set("MaxCupboards", 5); // Default value as integer
                Config.Save();
                Debug.Log("TCLimiter: Default config loaded and saved.");
            }
        }

        private void LoadConfigValues()
        {
            // Load config values
            maxCupboards = Config.Get<int>("MaxCupboards");
            Debug.Log("TCLimiter: Config values loaded.");
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (prefab.fullName.Contains("cupboard"))
            {
                BasePlayer player = planner.GetOwnerPlayer();
                if (player != null && !player.IsAdmin)
                {
                    var ownerID = player.userID;
                    int currentCupboards = placedCupboards.ContainsKey(ownerID) ? placedCupboards[ownerID] : 0;
                    int maxCupboards = Config.Get<int>("MaxCupboards");
        
                    if (currentCupboards >= maxCupboards)
                    {
                        player.ChatMessage("You have reached the maximum limit of tool cupboards. {currentCupboards}/{maxCupboards}");
                        return false;
                    }
                    else
                    {
                        return null; // Allow building since player hasn't reached the limit
                    }
                }
            }
            return null; // Allow building if not a tool cupboard
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            BasePlayer player = planner.GetOwnerPlayer();
            if (player != null && gameObject.name.Contains("cupboard"))
            {
                var ownerID = player.userID;
                if (!placedCupboards.ContainsKey(ownerID))
                {
                    placedCupboards[ownerID] = 1;
                }
                else
                {
                    placedCupboards[ownerID]++;
                }

                // Display a message to the player
                int currentCupboards = placedCupboards[ownerID];
                int maxCupboards = Config.Get<int>("MaxCupboards");
                player.ChatMessage($"You have placed {currentCupboards} out of {maxCupboards} tool cupboards.");
            }
        }

        void OnEntityKill(BaseEntity entity)
        {
            if (entity.ShortPrefabName.Contains("cupboard"))
            {
                var ownerID = entity.OwnerID;
                if (ownerID != 0 && placedCupboards.ContainsKey(ownerID))
                {
                    placedCupboards[ownerID]--;
                    //Debug.Log($"TCLimiter: Tool cupboard destroyed with owner ID {ownerID}.");
                }
            }
        }
    }
}
