//████████╗ █████╗  ██████╗███╗   ███╗ █████╗ ███╗   ██╗
//╚══██╔══╝██╔══██╗██╔════╝████╗ ████║██╔══██╗████╗  ██║
//   ██║   ███████║██║     ██╔████╔██║███████║██╔██╗ ██║
//   ██║   ██╔══██║██║     ██║╚██╔╝██║██╔══██║██║╚██╗██║
//   ██║   ██║  ██║╚██████╗██║ ╚═╝ ██║██║  ██║██║ ╚████║
//   ╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using System.IO;
using System.ComponentModel;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.1.1")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private const string permissionName = "compoststacks.use";

        private Dictionary<ulong, bool> composterData = new Dictionary<ulong, bool>();
        private const string dataFileName = "CompostStacksData";

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
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

                    composterData[ownerID] = composter.CompostEntireStack;
                }
                else
                {
                    if (config.debug)
                    {
                    Puts($"Owner player not found for OwnerID: {ownerID}");
                    }
                }
            }
        }

        private void UpdateComposters()
        {
            if (composterData != null && composterData.Count > 0)
            {
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
                foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
                {
                    ulong ownerID = composter.OwnerID;
                    IPlayer ownerPlayer = covalence.Players.FindPlayerById(ownerID.ToString());

                    if (ownerPlayer != null)
                    {
                        bool hasPermission = HasPermission(ownerPlayer);
                        composter.CompostEntireStack = hasPermission ? CompostEntireStack : false;

                        composterData[ownerID] = composter.CompostEntireStack;
                    }
                }
            }
        }

        private void OnUserPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to user {id}");
                UpdateComposters();
            }
        }

        private void OnUserPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} revoked for user {id}");
                UpdateComposters();
            }
        }

        private void OnGroupPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to group {id}");
                UpdateComposters();
            }
        }

        private void OnGroupPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} revoked for group {id}");
                UpdateComposters();
            }
        }

        private bool HasPermission(IPlayer player)
        {
            return player.HasPermission(permissionName);
        }


        #region Config
        static Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool debug;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
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
    }
}


