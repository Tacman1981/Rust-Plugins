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
    [Info("TransformHeliCrates", "Tacman", "0.1.0")]
    [Description("Moves heli crates to the player who destroyed the helicopter, with player opt-in.")]
    public class TransformHeliCrates : RustPlugin
    {
        private Dictionary<PatrolHelicopter, BasePlayer> heliKillers = new Dictionary<PatrolHelicopter, BasePlayer>();
        private Dictionary<ulong, DateTime> lastCommandUsage = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, bool> playerOptInStatus = new Dictionary<ulong, bool>();
        private const string PermissionBlockCommand = "transformhelicrates.block";
        private const string dataFilePath = "TransformHeliCrates/TransformHeliCrates";
        private const string usePerm = "transformhelicrates.use";
        private Dictionary<ulong, bool> hasSentMessage = new Dictionary<ulong, bool>();

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            permission.RegisterPermission(PermissionBlockCommand, this);
            LoadOptInData();
        }

        private void LoadOptInData()
        {
            playerOptInStatus = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>(dataFilePath) ?? new Dictionary<ulong, bool>();
        }

        private void SaveOptInData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(dataFilePath, playerOptInStatus);
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

        private Dictionary<uint, Timer> crateTimers = new Dictionary<uint, Timer>();

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity != null && (entity.ShortPrefabName == "heli_crate" || entity.ShortPrefabName == "codelockedhackablecrate"))
            {
                ulong crateId = entity.net?.ID.Value ?? 0;

                if (crateId != 0 && !crateTimers.ContainsKey((uint)crateId))
                {
                    crateTimers[(uint)crateId] = timer.Once(config.crateDespawn, () =>
                    {
                        if (entity != null && !entity.IsDestroyed)
                        {
                            entity.Kill();
                            crateTimers.Remove((uint)crateId);
                            Puts($"Crate with ID {crateId} despawned after {config.crateDespawn} seconds.");
                        }
                    });
                }

                NextTick(() => CheckOwnerAndMove(entity));
            }
        }

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


        private void CheckOwnerAndMove(BaseEntity entity)
        {
            if (!permission.UserHasPermission(entity.OwnerID.ToString(), usePerm)) return;

            // Opt-out notification logic
            if (!playerOptInStatus.TryGetValue(entity.OwnerID, out bool isOptedIn) || !isOptedIn)
            {
                return;
            }

            ulong ownerId = entity.OwnerID;
            BasePlayer owner = BasePlayer.FindByID(ownerId);

            if (owner == null) return;

            Vector3 playerPosition = owner.transform.position;

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

            rb.useGravity = true;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;

            entity.SendNetworkUpdateImmediate();

            // Puts($"Moved {entity.ShortPrefabName} to {owner.displayName}'s position: {entity.transform.position}");
        }

        void OnEntityKill(BaseNetworkable entity)
        {
            if (entity is BaseEntity baseEntity && crateTimers.ContainsKey((uint)baseEntity.net.ID.Value))
            {
                // Cancel and remove the despawn timer for this entity
                crateTimers[(uint)baseEntity.net.ID.Value]?.Destroy();
                crateTimers.Remove((uint)baseEntity.net.ID.Value);

                Puts($"Removed despawn timer for crate with ID: {baseEntity.net.ID.Value}");
            }
        }


        [ChatCommand("crates")]
        private void MoveAllCrates(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionBlockCommand) || !permission.UserHasPermission(player.UserIDString, usePerm))
            {
                player.ChatMessage("You do not have permission to use this command.");
                return;
            }

            if (!playerOptInStatus.TryGetValue(player.userID, out bool isOptedIn) || !isOptedIn)
            {
                player.ChatMessage("You have not opted in to use this command. Type /cratetoggle <true> to enable crate transform.");
                return;
            }

            if (lastCommandUsage.TryGetValue(player.userID, out DateTime lastUse) && (DateTime.Now - lastUse).TotalSeconds < config.coolDown)
            {
                player.ChatMessage($"Please wait {config.coolDown - (int)(DateTime.Now - lastUse).TotalSeconds} seconds before using this command again.");
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
            foreach (BaseEntity crate in crates)
            {
                ulong ownerId = crate.OwnerID;
                BasePlayer owner = BasePlayer.FindByID(ownerId);

                if (owner != null)
                {
                    float distance = Vector3.Distance(crate.transform.position, player.transform.position);

                    if (distance <= 10f)
                    {
                        CheckOwnerAndMove(crate);
                        movedCount++;
                    }
                    else
                    {
                        player.ChatMessage($"Crate {crate.ShortPrefabName} is too far from your position to move.");
                    }
                }
            }

            player.ChatMessage($"Moved {movedCount} crate(s) to Owners Position.");
            lastCommandUsage[player.userID] = DateTime.Now;
        }

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
