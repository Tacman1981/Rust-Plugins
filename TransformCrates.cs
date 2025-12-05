//████████╗ █████╗  ██████╗███╗   ███╗ █████╗ ███╗   ██╗
//╚══██╔══╝██╔══██╗██╔════╝████╗ ████║██╔══██╗████╗  ██║
//   ██║   ███████║██║     ██╔████╔██║███████║██╔██╗ ██║
//   ██║   ██╔══██║██║     ██║╚██╔╝██║██╔══██║██║╚██╗██║
//   ██║   ██║  ██║╚██████╗██║ ╚═╝ ██║██║  ██║██║ ╚████║
//   ╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝
using Oxide.Core;
using Oxide.Core.Plugins;
using Facepunch;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;


//This is an upgrade to transform heli crates, it now includes bradley crates too. also a command for each crate type .

namespace Oxide.Plugins
{
    [Info("TransformCrates", "Tacman", "0.2.5")]
    [Description("Moves heli crates to the player who destroyed the helicopter, with player opt-in.")]
    public class TransformCrates : RustPlugin
    {
        private Dictionary<ulong, DateTime> lastCommandUsage = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, bool> playerOptInStatus = new Dictionary<ulong, bool>();
        private const string PermissionBlockCommand = "transformcrates.block";
        private const string dataFilePath = "TransformCrates/TransformCrates";
        private const string usePerm = "transformcrates.hcrate";
        private const string usePerm2 = "transformcrates.bcrate";

        #region Data Initialization

        private void LoadOptInData()
        {
            playerOptInStatus = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(dataFilePath) ?? new Dictionary<ulong, bool>();

            foreach (var playerId in playerOptInStatus.Keys.ToList())
            {
                if (!playerOptInStatus.ContainsKey(playerId))
                {
                    playerOptInStatus[playerId] = true;
                    SaveOptInData();
                }
            }
        }

        private void SaveOptInData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(dataFilePath, playerOptInStatus);
        }

        #endregion

        #region Setup & Unload

        private Dictionary<uint, Timer> crateTimers = new Dictionary<uint, Timer>();

        void Unload()
        {
            foreach (var crateTimer in crateTimers.Values)
            {
                crateTimer?.Destroy();
            }
            crateTimers.Clear();
            SaveOptInData();
            lastCommandUsage.Clear();
        }

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            permission.RegisterPermission(PermissionBlockCommand, this);
            permission.RegisterPermission(usePerm2, this);
            LoadOptInData();
        }

        #endregion

        #region Helper Method
        private void CheckOwnerAndMove(BaseEntity entity)
        {
            if (!permission.UserHasPermission(entity.OwnerID.ToString(), usePerm) && !permission.UserHasPermission(entity.OwnerID.ToString(), usePerm2)) return;

            ulong ownerId = entity.OwnerID;
            BasePlayer owner = BasePlayer.FindByID(ownerId);

            if (owner == null || entity == null)
            {
                Puts($"No owner found for {entity}");
                return;
            }

            Vector3 playerPosition = owner.transform.position;

            float randomX = UnityEngine.Random.Range(config.xMin, config.xMax);
            float randomY = UnityEngine.Random.Range(2f, 4f);
            float randomZ = UnityEngine.Random.Range(config.zMin, config.zMax);

            entity.transform.position = playerPosition + new Vector3(randomX, randomY, randomZ);

            Rigidbody rb = entity.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = entity.gameObject.AddComponent<Rigidbody>();
            }

            rb.velocity = Vector3.zero;
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            //rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;

            entity.SendNetworkUpdate();

            //Puts($"Moved {entity.ShortPrefabName} to {owner.displayName}'s position: {entity.transform.position}");

            ulong crateId = entity.net?.ID.Value ?? 0;

            if (crateId != 0 && !crateTimers.ContainsKey((uint)crateId))
            {
                crateTimers[(uint)crateId] = timer.Once(config.crateDespawn, () =>
                {
                    if (entity != null && !entity.IsDestroyed)
                    {
                        entity.Kill();
                        crateTimers.Remove((uint)crateId);
                        //Puts($"Crate with ID {crateId} despawned after {config.crateDespawn} seconds.");
                    }
                });
            }
        }

        #endregion

        #region Hooks

        void OnUserPermissionGranted(string id, string permName)
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(permName)) return;

            if (permName.Equals(usePerm))
            {
                if (ulong.TryParse(id, out ulong userId))
                {
                    if (!playerOptInStatus.ContainsKey(userId) || !playerOptInStatus[userId])
                    {
                        playerOptInStatus[userId] = true;
                        SaveOptInData();
                        //Puts($"Player {userId} has been opted in because they were granted the {usePerm} permission.");
                    }
                }
                else
                {
                    Puts($"Invalid user ID format: {id}");
                }
            }
        }
        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity != null && ((entity.ShortPrefabName == "heli_crate" || entity.ShortPrefabName == "codelockedhackablecrate") || entity.ShortPrefabName == "bradley_crate"))
            {

                timer.Once(0.1f, () =>
                {
                    if (!playerOptInStatus.TryGetValue(entity.OwnerID, out bool isOptedIn) || !isOptedIn)
                    {
                        //player.ChatMessage("You have not opted in to use this command. Type /cratetoggle <true> to enable crate transform.");
                        return;
                    }
                    string ownerId = entity.OwnerID.ToString();
                    string prefab = entity.ShortPrefabName;

                    if (
                        (permission.UserHasPermission(ownerId, usePerm) &&
                            (prefab == "heli_crate" || prefab == "codelockedhackablecrate")) ||
                        (permission.UserHasPermission(ownerId, usePerm2) &&
                            (prefab == "bradley_crate" || prefab == "codelockedhackablecrate"))
                    )
                    {
                        CheckOwnerAndMove(entity);
                    }
                });

            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is StorageContainer crate && (entity.ShortPrefabName == "heli_crate" || entity.ShortPrefabName == "codelockedhackablecrate") || (entity.ShortPrefabName == "bradley_crate"))
            {
                if (crateTimers.ContainsKey((uint)entity.net.ID.Value))
                {
                    crateTimers[(uint)entity.net.ID.Value]?.Destroy();
                    crateTimers.Remove((uint)entity.net.ID.Value);
                    //Puts($"Removed despawn timer for crate with ID: {entity.net.ID.Value}");
                }
            }
        }


        #endregion

        #region Commands

        [ChatCommand("hcrates")]
        private void MoveAllCrates(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionBlockCommand) || !permission.UserHasPermission(player.UserIDString, usePerm))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            if (lastCommandUsage.TryGetValue(player.userID, out DateTime lastUse) && (DateTime.Now - lastUse).TotalSeconds < config.coolDown)
            {
                player.ChatMessage($"Please wait {(config.coolDown - (DateTime.Now - lastUse).TotalSeconds):F2} seconds before using this command again.");
                return;
            }

            List<BaseEntity> crates = new List<BaseEntity>();
            foreach (BaseEntity entity in BaseEntity.serverEntities)
            {
                if (entity is BaseEntity crate && (crate.ShortPrefabName == "heli_crate" || crate.ShortPrefabName == "codelockedhackablecrate"))
                {
                    crates.Add(crate);
                }
            }

            if (crates.Count == 0)
            {
                player.ChatMessage("No crates found within range.");
                return;
            }

            int movedCount = 0;
            float moveRadius = 20f; // Define the radius for moving crates

            foreach (BaseEntity crate in crates)
            {
                float distance = Vector3.Distance(crate.transform.position, player.transform.position);

                // Check if the crate is within the defined radius
                if (distance <= moveRadius)
                {
                    CheckOwnerAndMove(crate); // Move the crate if it has an owner, and only to the owners position.
                    movedCount++;
                }
            }

            if (movedCount == 0)
            {
                player.ChatMessage("No crates are close enough to move.");
            }
            else
            {
                player.ChatMessage($"Moved {movedCount} crate(s) to your position.");
            }

            lastCommandUsage[player.userID] = DateTime.Now;
        }

        [ChatCommand("bcrates")]
        private void MoveBradCrates(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionBlockCommand) || !permission.UserHasPermission(player.UserIDString, usePerm2))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            if (lastCommandUsage.TryGetValue(player.userID, out DateTime lastUse) && (DateTime.Now - lastUse).TotalSeconds < config.coolDown)
            {
                player.ChatMessage($"Please wait {(config.coolDown - (DateTime.Now - lastUse).TotalSeconds):F2} seconds before using this command again.");
                return;
            }

            List<BaseEntity> crates = new List<BaseEntity>();
            foreach (BaseEntity entity in BaseEntity.serverEntities)
            {
                if (entity is BaseEntity crate && (crate.ShortPrefabName == "codelockedhackablecrate" || crate.ShortPrefabName == "bradley_crate"))
                {
                    crates.Add(crate);
                }
            }

            if (crates.Count == 0)
            {
                player.ChatMessage("No crates found within range.");
                return;
            }

            int movedCount = 0;
            float moveRadius = 20f; // Define the radius for moving crates from

            foreach (BaseEntity crate in crates)
            {
                float distance = Vector3.Distance(crate.transform.position, player.transform.position);

                if (distance <= moveRadius)
                {
                    CheckOwnerAndMove(crate); // Move the crate to owner if it has 1, else crate stays put.
                    movedCount++;
                }
            }

            if (movedCount == 0)
            {
                player.ChatMessage("No crates are close enough to move.");
            }
            else
            {
                player.ChatMessage($"Moved {movedCount}  to your position.");
            }

            lastCommandUsage[player.userID] = DateTime.Now;
        }

        [ChatCommand("cratetoggle")]
        private void ToggleCrateOptIn(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 1 && bool.TryParse(args[0], out bool optIn))
            {
                playerOptInStatus[player.userID] = optIn;
                SaveOptInData();
                player.ChatMessage($"Crate-moving feature is now {(optIn ? "enabled" : "disabled")} for you.");
            }
            else
            {
                player.ChatMessage("Usage: /cratetoggle <true|false>");
            }
        }

        #endregion

        #region Config

        static Configuration config;
        public class Configuration
        {
            [JsonProperty("Cooldown on /crates command (in seconds)")]
            public int coolDown = 10;
            [JsonProperty("Teleport to player variance: Z Min")]
            public float zMin = -5;
            [JsonProperty("Teleport to player variance: Z Max")]
            public float zMax = 5;
            [JsonProperty("Teleport to player variance: X Min")]
            public float xMin = -5;
            [JsonProperty("Teleport to player variance: X MaX")]
            public float xMax = 5;
            [JsonProperty("Time until spawned crates despawn")]
            public float crateDespawn = 3600f;

            public static Configuration DefaultConfig() => new Configuration
            {
                coolDown = 10,
                zMin = -5f,
                zMax = 5f,
                xMin = -5f,
                xMax = 5f,
                crateDespawn = 3600f
            };

        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>() ?? Configuration.DefaultConfig();
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

