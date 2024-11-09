using Oxide.Core;
using Oxide.Core.Plugins;
using Facepunch;
using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("TransformHeliCrates", "Tacman", "0.0.15")]
    [Description("Moves heli crates to the player who destroyed the helicopter, with player opt-in.")]
    public class TransformHeliCrates : RustPlugin
    {
        private Dictionary<PatrolHelicopter, BasePlayer> heliKillers = new Dictionary<PatrolHelicopter, BasePlayer>();
        private Dictionary<ulong, DateTime> lastCommandUsage = new Dictionary<ulong, DateTime>();
        private Dictionary<ulong, bool> playerOptInStatus = new Dictionary<ulong, bool>();
        private const string PermissionBlockCommand = "transformhelicrates.block";
        private const string dataFilePath = "TransformHeliCrates/TransformHeliCrates";

        void Init()
        {
            permission.RegisterPermission(PermissionBlockCommand, this);
            LoadOptInData();
        }

        void Unload()
        {
            SaveOptInData();
            heliKillers.Clear();
            lastCommandUsage.Clear();
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

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity != null && (entity.ShortPrefabName == "heli_crate" || entity.ShortPrefabName == "codelockedhackablecrate"))
            {
                timer.Once(0.1f, () => CheckOwnerAndMove(entity));
            }
        }

        private void CheckOwnerAndMove(BaseEntity entity)
        {
            ulong ownerId = entity.OwnerID;
            BasePlayer owner = BasePlayer.FindByID(ownerId);

            // Opt-in check for automatic crate transform
            if (owner == null || !playerOptInStatus.GetValueOrDefault(ownerId, false))
            {
                return;
            }

            Vector3 playerPosition = owner.transform.position;
            entity.transform.position = playerPosition + new Vector3(0, 1.5f, 0);
            entity.SendNetworkUpdateImmediate();

            Puts($"Moved {entity.ShortPrefabName} to {owner.displayName}'s position: {entity.transform.position}");

            timer.Once(1f, () =>
            {
                Rigidbody rb = entity.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = entity.gameObject.AddComponent<Rigidbody>();
                }
                rb.useGravity = true;
            });
        }

        [ChatCommand("crates")]
        private void MoveAllCrates(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, PermissionBlockCommand))
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

            public static Configuration DefaultConfig() => new Configuration { coolDown = 10 };
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
