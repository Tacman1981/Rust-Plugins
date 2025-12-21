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


//Bug in the OnEntityKill being looked into, maybe this is fixed now..
//Crates despawn timer added in 0.2.5
//Added new position graber for player eyes in 0.2.6 since carbon acts weird with player.transform.position.
//Changed crate checks to use prefab ID instead of string matching in 0.3.0 for better performance.

namespace Oxide.Plugins
{
    [Info("TransformCrates", "Tacman", "0.3.0")]
    [Description("Moves heli and bradley crates to the relevant owner on kill, with player opt-in.")]
    public class TransformCrates : RustPlugin
    {
        private Dictionary<ulong, DateTime> lastCommandUsage = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, bool> playerOptInStatus = new Dictionary<ulong, bool>();
        private const string PermissionBlockCommand = "transformcrates.block";
        private const string dataFilePath = "TransformCrates/TransformCrates";
        private const string usePerm = "transformcrates.hcrate";
        private const string usePerm2 = "transformcrates.bcrate";
        private uint hcratePrefabID = 1314849795;
        private uint bcratePrefabID = 1737870479;
        private uint codelockPrefabID = 209286362;

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

            Vector3 playerPosition = owner.eyes.position;

            float randomX = UnityEngine.Random.Range(config.xMin, config.xMax);
            float randomY = UnityEngine.Random.Range(config.yMin, config.yMax);
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
            if (entity != null && ((entity.prefabID == hcratePrefabID || entity.prefabID == codelockPrefabID) || entity.prefabID == bcratePrefabID))
            {

                timer.Once(0.5f, () =>
                {
                    if (!playerOptInStatus.TryGetValue(entity.OwnerID, out bool isOptedIn) || !isOptedIn)
                    {
                        //player.ChatMessage("You have not opted in to use this command. Type /cratetoggle <true> to enable crate transform.");
                        return;
                    }
                    string ownerId = entity.OwnerID.ToString();
                    uint prefab = entity.prefabID;

                    if (
                        (permission.UserHasPermission(ownerId, usePerm) &&
                            (prefab == hcratePrefabID || prefab == codelockPrefabID)) ||
                        (permission.UserHasPermission(ownerId, usePerm2) &&
                            (prefab == bcratePrefabID || prefab == codelockPrefabID))
                    )
                    {
                        CheckOwnerAndMove(entity);
                    }
                });

            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is StorageContainer crate && (entity.prefabID == hcratePrefabID || entity.prefabID == codelockPrefabID) || (entity.prefabID == bcratePrefabID))
            {
                if (crateTimers.ContainsKey((uint)entity.net.ID.Value))
                {
                    crateTimers[(uint)entity.net.ID.Value]?.Destroy();
                    crateTimers.Remove((uint)entity.net.ID.Value);
                    //Puts($"Removed despawn timer for crate with ID: {entity.prefabID}");
                }
                //Puts($"crate is {entity.prefabID}");
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
                if (entity is BaseEntity crate && (crate.prefabID == codelockPrefabID || crate.prefabID == hcratePrefabID))
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
            float moveRadius = 30f; // Define the radius for moving crates

            foreach (BaseEntity crate in crates)
            {
                float distance = Vector3.Distance(crate.transform.position, player.eyes.position);

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
                if (entity is BaseEntity crate && (crate.prefabID == codelockPrefabID || crate.prefabID == bcratePrefabID))
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
            float moveRadius = 40f; // Define the radius for moving crates from

            foreach (BaseEntity crate in crates)
            {
                float distance = Vector3.Distance(crate.transform.position, player.eyes.position);

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
            [JsonProperty("Cooldown on /hcrates and /bcrates command (in seconds)")]
            public int coolDown = 10;
            [JsonProperty("Teleport to player variance: Z Min")]
            public float zMin = -5;
            [JsonProperty("Teleport to player variance: Z Max")]
            public float zMax = 5;
            [JsonProperty("Teleport to player variance: Y Max")]
            public float yMax = 2;
            [JsonProperty("Teleport to player variance: Y Min")]
            public float yMin = 1;
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
