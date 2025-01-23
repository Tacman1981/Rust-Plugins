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

namespace Oxide.Plugins
{
    [Info("TransformHeliCrates", "Tacman", "0.1.5")]
    [Description("Moves heli crates to the player who destroyed the helicopter, with player opt-in.")]
    public class TransformHeliCrates : RustPlugin
    {
        private Dictionary<PatrolHelicopter, BasePlayer> heliKillers = new Dictionary<PatrolHelicopter, BasePlayer>();
        private Dictionary<ulong, DateTime> lastCommandUsage = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, bool> playerOptInStatus = new Dictionary<ulong, bool>();
        private const string PermissionBlockCommand = "transformhelicrates.block";
        private const string dataFilePath = "TransformHeliCrates/TransformHeliCrates";
        private const string usePerm = "transformhelicrates.use";

        #region Data Initialization

        private void LoadOptInData()
        {
            // Load data
            playerOptInStatus = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(dataFilePath) ?? new Dictionary<ulong, bool>();

            // Ensure players who don't have an entry are assumed opted-in (default to true)
            foreach (var playerId in playerOptInStatus.Keys.ToList())
            {
                if (!playerOptInStatus.ContainsKey(playerId))
                {
                    playerOptInStatus[playerId] = true; // Assume opted in by default
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
            heliKillers.Clear();
            lastCommandUsage.Clear();
        }

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            permission.RegisterPermission(PermissionBlockCommand, this);
            LoadOptInData();
        }

        #endregion

        #region Helper Method
        private void CheckOwnerAndMove(BaseEntity entity)
        {
            if (!permission.UserHasPermission(entity.OwnerID.ToString(), usePerm)) return;

            if (!playerOptInStatus.TryGetValue(entity.OwnerID, out bool isOptedIn) || !isOptedIn)
            {
                //player.ChatMessage("You have not opted in to use this command. Type /cratetoggle <true> to enable crate transform.");
                return;
            }

            ulong ownerId = entity.OwnerID;
            BasePlayer owner = BasePlayer.FindByID(ownerId);

            // Opt-in check for automatic crate transform
            if (owner == null)
            {
                //return here if player query is not true
                return;
            }

            Vector3 playerPosition = owner.transform.position;

            // Generate random offsets
            float randomX = UnityEngine.Random.Range(config.xMin, config.xMax);
            float randomY = UnityEngine.Random.Range(2f, 4f);
            float randomZ = UnityEngine.Random.Range(config.zMin, config.zMax);

            // Apply the random offset to the position
            entity.transform.position = playerPosition + new Vector3(randomX, randomY, randomZ);

            Rigidbody rb = entity.gameObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = entity.gameObject.AddComponent<Rigidbody>();
            }

            // Configure the Rigidbody
            rb.useGravity = true; // Enable gravity if desired
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // Set collision detection mode
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezePositionX;

            entity.SendNetworkUpdateImmediate();

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

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is PatrolHelicopter helicopter && info.InitiatorPlayer != null)
            {
                heliKillers[helicopter] = info.InitiatorPlayer;
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is PatrolHelicopter helicopter)
            {
                heliKillers.Remove(helicopter);
            }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity != null && (entity.ShortPrefabName == "heli_crate" || entity.ShortPrefabName == "codelockedhackablecrate"))
            {
                timer.Once(0.1f, () => CheckOwnerAndMove(entity));
            }
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BaseEntity baseEntity && crateTimers.ContainsKey((uint)baseEntity.net.ID.Value))
            {
                // Cancel and remove the despawn timer for this entity
                crateTimers[(uint)baseEntity.net.ID.Value]?.Destroy();
                crateTimers.Remove((uint)baseEntity.net.ID.Value);

                //Puts($"Removed despawn timer for crate with ID: {baseEntity.net.ID.Value}");
            }
        }

        #endregion

        #region Commands

        [ChatCommand("crates")]
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
            BaseEntity closestCrate = null;
            float closestDistance = 5f;

            // Find the closest crate within the specified range
            foreach (BaseEntity crate in crates)
            {
                ulong ownerId = crate.OwnerID;
                BasePlayer owner = BasePlayer.FindByID(ownerId);

                if (owner != null)
                {
                    float distance = Vector3.Distance(crate.transform.position, player.transform.position);

                    if (distance <= 20f && distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestCrate = crate;
                    }
                }
            }

            // If we found a valid closest crate, move it
            if (closestCrate != null)
            {
                CheckOwnerAndMove(closestCrate);
                movedCount++;
            }
            else
            {
                player.ChatMessage("No crates are close enough to move.");
            }

            //player.ChatMessage($"Moved {movedCount} crate(s) to Owners Position.");
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
            [JsonProperty("Cooldown")]
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
