using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using System.IO;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.0.4")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private const string permissionName = "compoststacks.use"; // Permission name

        private Dictionary<ulong, bool> composterData = new Dictionary<ulong, bool>(); // Stores OwnerID and CompostEntireStack status
        private const string dataFileName = "CompostStacksData";

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
            LoadData();
            UpdateComposters(); // Update all composters based on loaded data
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter composter)
            {
                ulong ownerID = composter.OwnerID;
                IPlayer ownerPlayer = covalence.Players.FindPlayerById(ownerID.ToString());

                if (ownerPlayer != null)
                {
                    bool hasPermission = HasPermission(ownerPlayer);
                    composter.CompostEntireStack = hasPermission ? CompostEntireStack : false;

                    // Store the status in the dictionary
                    composterData[ownerID] = composter.CompostEntireStack;
                }
                else
                {
                    Puts($"Owner player not found for OwnerID: {ownerID}");
                }
            }
        }

        private void UpdateComposters()
        {
            if (composterData != null && composterData.Count > 0)
            {
                // Update existing composters from the data file
                foreach (var entry in composterData)
                {
                    IPlayer ownerPlayer = covalence.Players.FindPlayerById(entry.Key.ToString());

                    if (ownerPlayer != null)
                    {
                        foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter && ((Composter)x).OwnerID == entry.Key))
                        {
                            composter.CompostEntireStack = entry.Value;
                        }
                    }
                }
            }
            else
            {
                // Fallback: Iterate through all composters if data is not loaded
                foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
                {
                    ulong ownerID = composter.OwnerID;
                    IPlayer ownerPlayer = covalence.Players.FindPlayerById(ownerID.ToString());

                    if (ownerPlayer != null)
                    {
                        bool hasPermission = HasPermission(ownerPlayer);
                        composter.CompostEntireStack = hasPermission ? CompostEntireStack : false;

                        // Store the status in the dictionary
                        composterData[ownerID] = composter.CompostEntireStack;
                    }
                }
            }
        }

        private bool HasPermission(IPlayer player)
        {
            return player.HasPermission(permissionName);
        }

        private void LoadData()
        {
            composterData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(dataFileName) ?? new Dictionary<ulong, bool>();
        }

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(dataFileName, composterData);
        }

        private void Unload()
        {
            SaveData(); // Save data when plugin unloads
        }

        private void OnUserPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to user {id}");
                UpdateCompostersForUser(id); // Update composters instead of reloading the plugin
            }
        }

        private void OnUserPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} revoked for user {id}");
                UpdateCompostersForUser(id); // Update composters instead of reloading the plugin
            }
        }

        private void OnGroupPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to group {id}");
                UpdateCompostersForGroup(id); // Update composters for the group
            }
        }

        private void OnGroupPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} revoked for group {id}");
                UpdateCompostersForGroup(id); // Update composters for the group
            }
        }

        private void UpdateCompostersForUser(string userId)
        {
            ulong ownerID = ulong.Parse(userId);
            IPlayer ownerPlayer = covalence.Players.FindPlayerById(userId);

            if (ownerPlayer != null)
            {
                bool hasPermission = HasPermission(ownerPlayer);
                foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter && ((Composter)x).OwnerID == ownerID))
                {
                    composter.CompostEntireStack = hasPermission ? CompostEntireStack : false;
                    composterData[ownerID] = composter.CompostEntireStack;
                    UpdateComposters();
                }

                SaveData(); // Save the updated data
            }
        }

        private void UpdateCompostersForGroup(string groupId)
        {
            // Iterate over all connected players
            foreach (IPlayer player in covalence.Players.Connected)
            {
                // Check if the player has the permission directly or via the group
                if (player.HasPermission(permissionName) || permission.UserHasGroup(player.Id, groupId))
                {
                    UpdateCompostersForUser(player.Id);
                }
            }
        }
        
        private void OnNewSave()
        {
            Interface.Oxide.DataFileSystem.WriteObject(dataFileName, new Dictionary<ulong, bool>());
            Puts("New save detected. Clearing data file.");
        }
    }
}
