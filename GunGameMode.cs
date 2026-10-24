using Newtonsoft.Json;
using Oxide.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GunGameMode", "Tacman", "2.0.4")]
    [Description("A streamlined, action-packed Gun Game plugin for Rust.")]
    internal class GunGameMode : RustPlugin
    {
        #region State & Data
        private Dictionary<ulong, int> playerTiers = new Dictionary<ulong, int>();
        private Dictionary<ulong, bool> seen = new Dictionary<ulong, bool>();
        private Dictionary<ulong, bool> isImmune = new Dictionary<ulong, bool>();
        private Dictionary<ulong, ulong> lastKilledPlayer = new Dictionary<ulong, ulong>();
        private HashSet<ulong> recentlyProcessedDeaths = new HashSet<ulong>();
        private bool antiFarming = false;

        private class GunTier
        {
            public string Weapon;
            public int WeaponAmount;
            public string Ammo;
            public int AmmoAmount;

            public GunTier(string weapon, string ammo = null, int weaponAmount = 1, int ammoAmount = 1000)
            {
                Weapon = weapon;
                Ammo = ammo;
                WeaponAmount = weaponAmount;
                AmmoAmount = ammoAmount;
            }
        }

        private static readonly GunTier[] Tiers = new GunTier[]
        {
            // Tiers 1 - 25: Standard Defenses (10 syringes, 5 bandages, 5 barricades)
            new GunTier("smg.mp5", "ammo.pistol"),
            new GunTier("rifle.lr300", "ammo.rifle"),
            new GunTier("rifle.semiauto", "ammo.rifle"),
            new GunTier("shotgun.double", "ammo.shotgun.slug"),
            new GunTier("pistol.semiauto", "ammo.pistol"),
            new GunTier("pistol.revolver", "ammo.pistol"),
            new GunTier("rifle.ak", "ammo.rifle"),
            new GunTier("pistol.python", "ammo.pistol"),
            new GunTier("lmg.m249", "ammo.rifle"),
            new GunTier("pistol.nailgun", "ammo.nailgun.nails"),
            new GunTier("grenade.f1", null, 1000, 0),
            new GunTier("smg.thompson", "ammo.pistol"),
            new GunTier("minicrossbow", "arrow.wooden"),
            new GunTier("rifle.bolt", "ammo.rifle"),
            new GunTier("shotgun.pump", "ammo.shotgun"),
            new GunTier("t1_smg", "ammo.pistol"),
            new GunTier("revolver.hc", "ammo.rifle"),
            new GunTier("shotgun.m4", "ammo.shotgun.fire"),
            new GunTier("pistol.prototype17", "ammo.pistol"),
            new GunTier("shotgun.waterpipe", "ammo.shotgun.slug"),
            new GunTier("blowpipe", "dart.wood"),
            new GunTier("bow.hunting", "arrow.wooden"),
            new GunTier("rifle.sks", "ammo.rifle"),
            new GunTier("hmlmg", "ammo.rifle"),
            new GunTier("bow.compound", "arrow.wooden"),

            // Tiers 26 - 50: Hard Mode (5 syringes, 2 bandages, 2 barricades)
            new GunTier("smg.mp5", "ammo.pistol"),
            new GunTier("rifle.lr300", "ammo.rifle"),
            new GunTier("rifle.semiauto", "ammo.rifle"),
            new GunTier("shotgun.double", "ammo.shotgun.slug"),
            new GunTier("pistol.semiauto", "ammo.pistol"),
            new GunTier("pistol.revolver", "ammo.pistol"),
            new GunTier("rifle.ak", "ammo.rifle"),
            new GunTier("pistol.python", "ammo.pistol"),
            new GunTier("lmg.m249", "ammo.rifle"),
            new GunTier("pistol.nailgun", "ammo.nailgun.nails"),
            new GunTier("grenade.f1", null, 1000, 0),
            new GunTier("smg.thompson", "ammo.pistol"),
            new GunTier("minicrossbow", "arrow.wooden"),
            new GunTier("rifle.bolt", "ammo.rifle"),
            new GunTier("shotgun.pump", "ammo.shotgun"),
            new GunTier("t1_smg", "ammo.pistol"),
            new GunTier("revolver.hc", "ammo.rifle"),
            new GunTier("shotgun.m4", "ammo.shotgun.fire"),
            new GunTier("pistol.prototype17", "ammo.pistol"),
            new GunTier("shotgun.waterpipe", "ammo.shotgun.slug"),
            new GunTier("pistol.m92", "ammo.pistol"),
            new GunTier("rifle.m39", "ammo.rifle"),
            new GunTier("rifle.sks", "ammo.rifle"),
            new GunTier("hmlmg", "ammo.rifle"),
            new GunTier("bow.compound", "arrow.wooden")
        };

        private static readonly int[] ArmorItemIDs = new int[]
        {
            -194953424,   // Metal Facemask
            1110385766,   // Metal Chestplate
            1850456855,   // Roadsign Kilt
            1751045826,   // Hoodie
            237239288,    // Pants
            -1549739227,  // Boots
            -1108136649   // Tactical Gloves
        };

        private static readonly ulong[] NormalArmorSkins = new ulong[]
        {
            3531335213, 3531334445, 3531337001, 3531324895, 3531335745, 3531341554
        };

        private static readonly ulong[] GoldenArmorSkins = new ulong[]
        {
            2908075832, 2908075221, 2908078759, 2908070670, 2908058854, 2908084112
        };
        #endregion

        #region Configuration
        private static Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool debug = false;

            [JsonProperty(PropertyName = "Chat Messages")]
            public bool chatmessages = true;

            [JsonProperty(PropertyName = "Immunity after respawning (Prevents spawn killing)")]
            public bool immunity = true;

            [JsonProperty(PropertyName = "Immunity Duration (seconds)")]
            public float immunityDuration = 4f;

            [JsonProperty(PropertyName = "Minimum Players for Anti-Farming")]
            public int minPlayersForAntiFarm = 6;

            [JsonProperty(PropertyName = "Automatically enable Anti-Farming based on player count")]
            public bool autoAntiFarm = false;

            [JsonProperty(PropertyName = "Wall Despawn Time (seconds)")]
            public float wallDespawnTime = 60f;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    debug = false,
                    chatmessages = true,
                    immunity = true,
                    immunityDuration = 4f,
                    minPlayersForAntiFarm = 6,
                    autoAntiFarm = false,
                    wallDespawnTime = 60f
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
                PrintWarning("Creating new configuration file. Please wait a moment...");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Hooks
        private void Init()
        {
            if (config.immunityDuration <= 0f) config.immunityDuration = 4f;
            if (config.minPlayersForAntiFarm <= 0) config.minPlayersForAntiFarm = 6;
            if (config.wallDespawnTime < 5f) config.wallDespawnTime = 10f;
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            LoadData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player == null || player.IsSleeping()) continue;
                GiveLoadout(player, GetPlayerTier(player.userID));
            }

            Server.Command("env.progresstime", "false");
            Server.Command("env.time", "12");
            Server.Command("server.events", "false");
            Server.Command("deepsea.enabled", "false");
            UpdateAntiFarmingStatus();
        }

        private void Unload()
        {
            lastKilledPlayer.Clear();
            isImmune.Clear();
            recentlyProcessedDeaths.Clear();
            SaveData();
        }

        private void OnServerSave() => SaveData();

        private void OnNewSave(string filename)
        {
            playerTiers.Clear();
            seen.Clear();
            SaveData();
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player == null) return;

            int tier = GetPlayerTier(player.userID);
            GiveLoadout(player, tier);
            SaveData();

            if (config.chatmessages)
                player.ChatMessage("Welcome to Gun Game! Eliminate opponents to level up your kit. Type /gghelp for commands.");

            Server.Broadcast($"{player.displayName} has joined the Gun Game!");
            UpdateAntiFarmingStatus();
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null) return;
            player.inventory.Strip();
            player.Die();
            UpdateAntiFarmingStatus();
        }

        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null) return;
            int tier = GetPlayerTier(player.userID);
            GiveLoadout(player, tier);

            player.health = player.MaxHealth();
            player.metabolism.calories.value = player.metabolism.calories.max;
            player.metabolism.hydration.value = player.metabolism.hydration.max;
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null || !config.immunity) return;

            isImmune[player.userID] = true;
            if (config.chatmessages)
            {
                player.ChatMessage($"You are immune for {config.immunityDuration} seconds or until you attack someone.");
            }

            timer.Once(config.immunityDuration, () =>
            {
                if (player != null && isImmune.TryGetValue(player.userID, out bool immune) && immune)
                {
                    isImmune[player.userID] = false;
                    if (config.chatmessages)
                        player.ChatMessage("Spawn immunity expired. Good luck!");
                }
            });
        }

        private void OnEntitySpawned(BaseCombatEntity entity)
        {
            if (entity == null) return;
            // Wood cover barricade prefab
            if (entity.prefabID == 2982625522)
            {
                BasePlayer player = BasePlayer.FindByID(entity.OwnerID);
                if (player != null && config.chatmessages)
                {
                    player.ChatMessage($"Barrier will despawn in {config.wallDespawnTime} seconds.");
                }
                timer.Once(config.wallDespawnTime, () =>
                {
                    if (entity != null && !entity.IsDestroyed)
                    {
                        entity.Kill();
                    }
                });
            }
        }

        private void OnLootEntity(BasePlayer player, BaseEntity entity)
        {
            if (entity is RidableHorse || entity is HorseCorpse) return;
            if (entity is BuildingPrivlidge || entity is BuildingPrivilegeRetro) return;
            if (entity is PlayerCorpse || entity is BasePlayer) return;
            entity.Kill();
        }

        private bool CanLootPlayer(BasePlayer target, BasePlayer looter)
        {
            looter.ChatMessage("You cannot loot other players in Gun Game!");
            return false;
        }

        private bool CanDropActiveItem(BasePlayer player) => false;

        private void OnPlayerCorpseSpawned(BasePlayer player, PlayerCorpse corpse)
        {
            if (corpse != null) corpse.Kill();
        }

        private bool CanBeWounded(BasePlayer player, HitInfo info) => false;

        private object OnPlayerWound(BasePlayer player, HitInfo info)
        {
            if (player == null) return null;
            HandleDeath(player, info);
            player.Die(info);
            return false;
        }

        private object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is not BasePlayer victim)
                return null;

            BasePlayer attacker = info?.Initiator as BasePlayer;

            // Handle self-damage
            if (attacker != null && victim == attacker)
            {
                if (victim.health <= info.damageTypes.Total())
                {
                    HandleDeath(victim, info);
                    victim.Die(info);
                    return false;
                }
                return null;
            }

            if (attacker == null)
                return null;

            // Attacking removes spawn immunity immediately
            if (isImmune.TryGetValue(attacker.userID, out bool attackerImmune) && attackerImmune)
            {
                isImmune[attacker.userID] = false;
                attacker.ChatMessage("Your spawn immunity was removed because you attacked someone!");
            }

            // Victim spawn immunity check
            if (config.immunity && isImmune.TryGetValue(victim.userID, out bool victimImmune) && victimImmune)
            {
                info.damageTypes.ScaleAll(0f);
                attacker.ChatMessage($"You hit {victim.displayName}, but they are currently immune after respawning!");
                return false;
            }

            // Anti-farming check (only active when server player count meets minimum)
            if (antiFarming && BasePlayer.activePlayerList.Count >= config.minPlayersForAntiFarm && lastKilledPlayer.TryGetValue(attacker.userID, out ulong lastVictim) && lastVictim == victim.userID)
            {
                info.damageTypes.ScaleAll(0f);
                attacker.ChatMessage($"Anti-farming: You cannot harm {victim.displayName} until you eliminate someone else!");
                return false;
            }

            // Custom damage scales: knife instant kill, arrows x2
            float stab = info.damageTypes.Get(Rust.DamageType.Stab);
            if (stab > 0f)
            {
                info.damageTypes.Set(Rust.DamageType.Stab, stab * 100f);
            }

            float arrow = info.damageTypes.Get(Rust.DamageType.Arrow);
            if (arrow > 0f)
            {
                info.damageTypes.Set(Rust.DamageType.Arrow, arrow * 2f);
            }

            victim.metabolism.bleeding.value = 0f;

            // Intercept lethal blow directly
            if (victim.health <= info.damageTypes.Total())
            {
                HandleDeath(victim, info);
                victim.Die(info);
                return false;
            }

            return null;
        }

        private void OnPlayerDied(BasePlayer victim, HitInfo info)
        {
            if (victim == null || victim.IsSleeping()) return;
            HandleDeath(victim, info);
        }

        private void HandleDeath(BasePlayer victim, HitInfo info)
        {
            if (victim == null) return;
            if (recentlyProcessedDeaths.Contains(victim.userID)) return;

            recentlyProcessedDeaths.Add(victim.userID);
            timer.Once(0.5f, () => recentlyProcessedDeaths.Remove(victim.userID));

            victim.inventory.Strip();

            BasePlayer attacker = info?.Initiator as BasePlayer;

            // F1 console kill or environmental suicide (no attacker or info is null)
            if (info == null || attacker == null)
            {
                ReduceTier(victim);
                if (config.chatmessages)
                {
                    victim.ChatMessage("<color=#ff5555>You died by suicide and lost a tier!</color>");
                }
                return;
            }

            // Self-kill (attacker == victim)
            if (attacker == victim)
            {
                bool isExplosion = info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Explosion;
                if (!isExplosion)
                {
                    ReduceTier(victim);
                    if (config.chatmessages)
                    {
                        victim.ChatMessage("<color=#ff5555>You committed suicide and lost a tier!</color>");
                    }
                }
                else if (config.chatmessages)
                {
                    victim.ChatMessage("<color=#ffa500>You died to your own explosion. Tier preserved!</color>");
                }
                return;
            }

            // Standard PvP elimination
            float distance = Vector3.Distance(attacker.transform.position, victim.transform.position);
            Item activeItem = attacker.GetActiveItem();
            string weaponName = activeItem?.info?.displayName?.translated ?? (info?.Weapon?.name ?? "Unknown Weapon");
            string hitLocation = !string.IsNullOrEmpty(info?.boneName) ? $" ({info.boneName})" : "";

            if (config.chatmessages)
            {
                victim.ChatMessage($"You were killed by {attacker.displayName} from {distance:F1}m with {weaponName}{hitLocation}!");
                attacker.ChatMessage($"You killed {victim.displayName} from {distance:F1}m with {weaponName}{hitLocation}!");
            }

            // Anti-farming tracking
            lastKilledPlayer[attacker.userID] = victim.userID;
            attacker.metabolism.bleeding.value = 0f;

            // Knife kill humiliation (Stab) demotes victim 1 tier and does NOT progress attacker's tier
            bool isKnifeKill = (info != null && info.damageTypes.GetMajorityDamageType() == Rust.DamageType.Stab);
            if (isKnifeKill)
            {
                ReduceTier(victim);
                victim.ChatMessage($"<color=#ff5555>You were knifed by {attacker.displayName} and demoted a tier!</color>");
                return;
            }

            // Progress attacker tier
            ProgressTier(attacker);
        }
        #endregion

        #region Helpers & Progression
        private int GetPlayerTier(ulong userId)
        {
            if (playerTiers.TryGetValue(userId, out int tier))
            {
                return Mathf.Clamp(tier, 1, 51);
            }
            playerTiers[userId] = 1;
            return 1;
        }

        private void ProgressTier(BasePlayer player)
        {
            if (player == null) return;
            int current = GetPlayerTier(player.userID);
            if (current >= 51)
            {
                if (!seen.TryGetValue(player.userID, out bool hasSeen) || !hasSeen)
                {
                    seen[player.userID] = true;
                    player.ChatMessage("Congratulations on reaching the final kit! Keep playing to show off your skills!");
                }
                return;
            }

            current++;
            playerTiers[player.userID] = current;
            SaveData();

            if (current == 26)
            {
                player.ChatMessage("<color=#ffa500>Difficulty increased! Defensive items have been halved.</color>");
            }

            GiveLoadout(player, current);
            player.ChatMessage($"<color=#55ff55>Promoted to Tier {current} / 51!</color>");
        }

        private void ReduceTier(BasePlayer player)
        {
            if (player == null) return;
            int current = GetPlayerTier(player.userID);
            if (current <= 1) return;

            current--;
            playerTiers[player.userID] = current;
            SaveData();

            GiveLoadout(player, current);
            player.ChatMessage($"<color=#ff5555>Demoted to Tier {current} / 51!</color>");
        }

        private void GiveLoadout(BasePlayer player, int tier)
        {
            if (player == null || !player.UserIDString.IsSteamId()) return;

            player.inventory.Strip();

            // Wearable armor
            bool isFinal = (tier >= 51);
            ulong[] skinIds = isFinal ? GoldenArmorSkins : NormalArmorSkins;

            for (int i = 0; i < ArmorItemIDs.Length; i++)
            {
                ulong skin = (i < skinIds.Length) ? skinIds[i] : 0;
                Item armorItem = ItemManager.CreateByItemID(ArmorItemIDs[i], 1, skin);
                if (armorItem != null)
                {
                    if (!armorItem.MoveToContainer(player.inventory.containerWear))
                        armorItem.Remove();
                }
            }

            if (isFinal)
            {
                if (!seen.TryGetValue(player.userID, out bool hasSeen) || !hasSeen)
                {
                    seen[player.userID] = true;
                    player.ChatMessage("Congratulations on reaching the final kit! Keep playing to show off your skills!");
                }

                GiveItem(player, "knife.combat", 1);
                GiveItem(player, "rocket.launcher", 1);
                GiveItem(player, "ammo.rocket.basic", 1000);
                GiveItem(player, "ammo.rocket.hv", 1000);
                GiveItem(player, "crossbow", 1);
                GiveItem(player, "arrow.wooden", 1000);
                GiveItem(player, "shotgun.m4", 1);
                GiveItem(player, "ammo.shotgun.fire", 1000);
                GiveItem(player, "ammo.shotgun", 1000);
                GiveItem(player, "rifle.lr300", 1);
                GiveItem(player, "ammo.rifle", 1000);
                GiveItem(player, "pistol.eoka", 1);
                GiveItem(player, "ammo.shotgun.slug", 1000);
                GiveItem(player, "military flamethrower", 10);
                GiveItem(player, "lowgradefuel", 1000);
                GiveItem(player, "bandage", 5);
                GiveItem(player, "barricade.wood.cover", 5);
            }
            else
            {
                int index = Mathf.Clamp(tier - 1, 0, Tiers.Length - 1);
                GunTier gt = Tiers[index];

                GiveItem(player, gt.Weapon, gt.WeaponAmount);
                GiveItem(player, "knife.combat", 1);
                if (!string.IsNullOrEmpty(gt.Ammo) && gt.AmmoAmount > 0)
                {
                    GiveItem(player, gt.Ammo, gt.AmmoAmount);
                }

                // Defensive items: Tiers 1-25 full, Tiers 26-50 halved
                int syringes = (tier <= 25) ? 10 : 5;
                int bandages = (tier <= 25) ? 5 : 2;
                int barricades = (tier <= 25) ? 5 : 2;

                GiveItem(player, "syringe.medical", syringes);
                GiveItem(player, "bandage", bandages);
                GiveItem(player, "barricade.wood.cover", barricades);
            }

            ReloadActiveGun(player);
        }

        private void GiveItem(BasePlayer player, string shortname, int amount)
        {
            Item item = ItemManager.CreateByName(shortname, amount);
            if (item != null && !player.inventory.GiveItem(item))
            {
                item.Remove();
            }
        }

        private void ReloadActiveGun(BasePlayer player)
        {
            timer.Once(0.1f, () =>
            {
                if (player == null || player.IsDead()) return;
                Item item = player.inventory.containerBelt.GetSlot(0);
                if (item != null)
                {
                    player.UpdateActiveItem(item.uid);
                    if (item.GetHeldEntity() is BaseProjectile bp && bp.primaryMagazine != null)
                    {
                        bp.primaryMagazine.contents = bp.primaryMagazine.capacity;
                        bp.SendNetworkUpdateImmediate();
                    }
                }
            });
        }

        private void UpdateAntiFarmingStatus()
        {
            if (!config.autoAntiFarm) return;
            antiFarming = BasePlayer.activePlayerList.Count >= config.minPlayersForAntiFarm;
        }
        #endregion

        #region Data Management
        private void SaveData()
        {
            var export = new Dictionary<ulong, string>();
            foreach (var kvp in playerTiers)
            {
                export[kvp.Key] = $"kit{kvp.Value}";
            }
            Interface.Oxide.DataFileSystem.WriteObject("GunGame/GunGameKits", export);
            Interface.Oxide.DataFileSystem.WriteObject("GunGame/GunGameSeen", seen);
        }

        private void LoadData()
        {
            try
            {
                var rawKits = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, string>>("GunGame/GunGameKits");
                playerTiers.Clear();
                if (rawKits != null)
                {
                    foreach (var kvp in rawKits)
                    {
                        if (int.TryParse(kvp.Value.Replace("kit", ""), out int tier))
                        {
                            playerTiers[kvp.Key] = Mathf.Clamp(tier, 1, 51);
                        }
                    }
                }
            }
            catch
            {
                playerTiers = new Dictionary<ulong, int>();
            }

            seen = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, bool>>("GunGame/GunGameSeen") ?? new Dictionary<ulong, bool>();
        }
        #endregion

        #region Commands
        [ChatCommand("ggreset")]
        private void ResetCmd(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            int current = GetPlayerTier(player.userID);
            if (current <= 1)
            {
                player.ChatMessage("You are already on the first kit, no need to reset.");
                return;
            }

            playerTiers[player.userID] = 1;
            seen[player.userID] = false;
            GiveLoadout(player, 1);
            SaveData();

            player.ChatMessage("Your Gun Game progress has been reset to Tier 1.");
        }

        [ChatCommand("ggstats")]
        private void StatsCmd(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            int current = GetPlayerTier(player.userID);
            player.ChatMessage($"You are currently on Tier {current} / 51.");
            player.ChatMessage($"You have {51 - current} kits left to complete the Gun Game.");
        }

        [ChatCommand("ggscore")]
        private void ScoreboardCmd(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            var scoreboard = new List<(string name, int tier)>();

            foreach (var entry in playerTiers)
            {
                var p = BasePlayer.FindByID(entry.Key);
                if (p != null)
                {
                    scoreboard.Add((p.displayName, entry.Value));
                }
            }

            scoreboard.Sort((a, b) => b.tier.CompareTo(a.tier));

            player.ChatMessage("Gun Game Top 10 Scoreboard:");
            for (int i = 0; i < Math.Min(10, scoreboard.Count); i++)
            {
                var entry = scoreboard[i];
                player.ChatMessage($"{i + 1}. {entry.name}: Tier {entry.tier}");
            }
        }

        [ChatCommand("gghelp")]
        private void HelpCmd(BasePlayer player, string command, string[] args)
        {
            if (player == null) return;
            player.ChatMessage("Gun Game Commands:");
            player.ChatMessage("/ggreset - Reset your Gun Game progress back to Tier 1.");
            player.ChatMessage("/ggstats - View your current tier and progress.");
            player.ChatMessage("/ggscore - View the top 10 players on the scoreboard.");
            player.ChatMessage("/gghelp - Show this help message.");
        }
        #endregion
    }
}
