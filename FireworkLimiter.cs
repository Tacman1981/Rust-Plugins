using System.Collections.Generic;
using Oxide.Core;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("Firework Limit", "Tacman", "1.0.0")]
    class FireworkLimiter : RustPlugin
    {
        private Dictionary<ulong, int> placedFireworks = new Dictionary<ulong, int>();
        private int maxFireworks;

        void OnServerInitialized()
        {
            LoadConfigValues();
            DetectExistingFireworks();
            Debug.Log("FireworkLimiter: Plugin Initialized.");
        }

        private void DetectExistingFireworks()
        {
            // Iterate through all entities in the game world
            foreach (var entity in BaseNetworkable.serverEntities)
            {
                if (entity == null) continue;

                // Check if the entity is a firework
                if (entity.ShortPrefabName.Contains("volcano") || entity.ShortPrefabName.Contains("mortar") || entity.ShortPrefabName.Contains("roman"))
                {
                    var baseEntity = entity as BaseEntity;
                    if (baseEntity != null)
                    {
                        var ownerID = baseEntity.OwnerID;
                        if (!placedFireworks.ContainsKey(ownerID))
                        {
                            placedFireworks[ownerID] = 1;
                        }
                        else
                        {
                            placedFireworks[ownerID]++;
                        }
                        // Debug.Log($"FireworkLimiter: Detected existing firework with owner ID {ownerID}.");
                    }
                }
            }
        }

        protected override void LoadDefaultConfig()
        {
            // Set default values only if the config doesn't exist
            if (Config.Get("MaxFireworks") == null)
            {
                Config.Set("MaxFireworks", 5); // Default value as integer
                Config.Save();
                Debug.Log("FireworkLimiter: Default config loaded and saved.");
            }
        }

        private void LoadConfigValues()
        {
            // Load config values
            maxFireworks = Config.Get<int>("MaxFireworks");
            Debug.Log("FireworkLimiter: Config values loaded.");
        }

        object CanBuild(Planner planner, Construction prefab, Construction.Target target)
        {
            if (prefab.fullName.Contains("volcano") || prefab.fullName.Contains("mortar") || prefab.fullName.Contains("roman"))
            {
                BasePlayer player = planner.GetOwnerPlayer();
                if (player != null && !player.IsAdmin)
                {
                    var ownerID = player.userID;
                    int currentFireworks = placedFireworks.ContainsKey(ownerID) ? placedFireworks[ownerID] : 0;

                    if (currentFireworks >= maxFireworks)
                    {
                        player.ChatMessage($"You have reached the maximum limit of fireworks. {currentFireworks}/{maxFireworks}");
                        return false;
                    }
                    else
                    {
                        return null; // Allow building since player hasn't reached the limit
                    }
                }
            }
            return null; // Allow building if not a firework or if the player is an admin
        }

        void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            var entity = gameObject.ToBaseEntity();
            string shortPrefabName = entity.ShortPrefabName.ToLower(); // Convert to lowercase for case-insensitive comparison
            if (shortPrefabName.Contains("volcano") || shortPrefabName.Contains("mortar") || shortPrefabName.Contains("roman"))
            {
                BasePlayer player = planner.GetOwnerPlayer();
                if (player != null && !player.IsAdmin)
                {
                    var ownerID = player.userID;
                    int currentFireworks = placedFireworks.ContainsKey(ownerID) ? placedFireworks[ownerID] : 0;
        
                    if (currentFireworks >= maxFireworks)
                    {
                        player.ChatMessage($"You have reached the maximum limit of fireworks. {currentFireworks}/{maxFireworks}");
                    }
                    else
                    {
                        if (!placedFireworks.ContainsKey(ownerID))
                        {
                            placedFireworks[ownerID] = 1;
                        }
                        else
                        {
                            placedFireworks[ownerID]++;
                        }
        
                        // Display a message to the player
                        currentFireworks = placedFireworks[ownerID];
                        player.ChatMessage($"You have placed {currentFireworks} out of {maxFireworks} fireworks.");
                        //Debug.Log($"FireworkLimiter: Player {player.userID} placed a firework entity ({shortPrefabName}).");
                    }
                }
            }
        }

        void OnEntityKill(BaseEntity entity)
        {
            if (entity.ShortPrefabName.Contains("volcano") || entity.ShortPrefabName.Contains("mortar") || entity.ShortPrefabName.Contains("roman"))
            {
                var ownerID = entity.OwnerID;
                if (ownerID != 0 && placedFireworks.ContainsKey(ownerID))
                {
                    placedFireworks[ownerID]--;
                    // Debug.Log($"FireworkLimiter: Firework destroyed with owner ID {ownerID}.");
                }
            }
        }
    }
}
