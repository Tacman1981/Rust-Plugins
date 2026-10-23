using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GhostKnocker", "Tacman", "1.10.0")]
    [Description("Halloween event: Knocks on doors, alerts residents, and awards Skill Tree XP with time-decay for fast reactions.")]
    public class GhostKnocker : RustPlugin
    {
        private const string AdminPermission = "ghostknocker.admin";

        [PluginReference]
        private Plugin SkillTree;

        #region Configuration

        private Configuration _config;

        private class Configuration
        {
            [JsonProperty(PropertyName = "Interval Between Knocks (Seconds)")]
            public float KnockIntervalSeconds = 120f;

            [JsonProperty(PropertyName = "Use Random Interval (Min/Max)")]
            public bool UseRandomInterval = false;

            [JsonProperty(PropertyName = "Min Interval (Seconds, if Use Random Interval is true)")]
            public float MinIntervalSeconds = 180f;

            [JsonProperty(PropertyName = "Max Interval (Seconds, if Use Random Interval is true)")]
            public float MaxIntervalSeconds = 600f;

            [JsonProperty(PropertyName = "Only Knock When Active Player Nearby")]
            public bool RequirePlayerNearby = true;

            [JsonProperty(PropertyName = "Nearby Player Hearing Distance (Meters)")]
            public float PlayerAudibleDistance = 15f;

            [JsonProperty(PropertyName = "Only Knock On Closed Doors")]
            public bool OnlyClosedDoors = true;

            [JsonProperty(PropertyName = "Enable Trick Or Treat Event")]
            public bool EnableEvent = true;

            [JsonProperty(PropertyName = "Event Time Limit To Answer Door (Seconds)")]
            public float EventTimeLimitSeconds = 60f;

            [JsonProperty(PropertyName = "Enable XP Time Decay")]
            public bool EnableXPDecay = true;

            [JsonProperty(PropertyName = "XP Decay Grace Period (Seconds, full XP before decay)")]
            public float XPDecayGraceSeconds = 10f;

            [JsonProperty(PropertyName = "XP Decay Percent Per Second After Grace Period")]
            public float XPDecayPercentPerSecond = 2f;

            [JsonProperty(PropertyName = "Minimum XP Floor (Lowest XP possible after decay)")]
            public double MinimumXPFloor = 10.0;

            [JsonProperty(PropertyName = "Announce Knock To Owner And Nearby Teammates")]
            public bool NotifyOnKnock = true;

            [JsonProperty(PropertyName = "Knock Announcement Message")]
            public string KnockAnnouncementMessage = "<color=#ffa500>[Trick or Treat]</color> 👻 <color=#ffff55>A ghost just knocked on one of your doors!</color> Open it within <color=#ff4444>{time}s</color>! (Full XP in first 10s, decays 2%/s after!)";

            [JsonProperty(PropertyName = "Reward Win Message")]
            public string RewardWinMessage = "<color=#ffa500>[Trick or Treat]</color> 🎃 <color=#55ff55>Congratulations!</color> You answered the ghost's door in time!";

            [JsonProperty(PropertyName = "Teammates Announcement When Door Is Claimed")]
            public string RangeClaimMessage = "<color=#ffa500>[Trick or Treat]</color> 🎃 Congratulations to <b>{player}</b>! They answered the door in <b>{time}s</b> and claimed <b>+{xp} Skill Tree XP</b>!";

            [JsonProperty(PropertyName = "Skill Tree Base XP Reward (0 to disable)")]
            public double SkillTreeXPAmount = 1000.0;

            [JsonProperty(PropertyName = "Night Time Bonus Multiplier (1.0 = off or auto-use SkillTree)")]
            public float NightBonusMultiplier = 3.0f;

            [JsonProperty(PropertyName = "Play Celebration Effect On Correct Door")]
            public bool PlayCelebrationEffect = true;

            [JsonProperty(PropertyName = "Reward Effect Prefab")]
            public string RewardEffectPrefab = "assets/prefabs/deployable/pooltable/effects/pooltable.confetti.prefab";

            [JsonProperty(PropertyName = "Broadcast Winner To Entire Server Chat")]
            public bool BroadcastRewardToChat = false;

            [JsonProperty(PropertyName = "Enable Date Gate (2 Weeks Ending Nov 1)")]
            public bool EnableDateGate = true;

            [JsonProperty(PropertyName = "Event Start Date (UTC)")]
            public string EventStartDate = "2026-10-18";

            [JsonProperty(PropertyName = "Event End Date (UTC)")]
            public string EventEndDate = "2026-11-01";

            [JsonProperty(PropertyName = "Data Auto-Save Interval (Minutes)")]
            public float DataAutoSaveMinutes = 5f;

            [JsonProperty(PropertyName = "Log Knocks To Server Console")]
            public bool LogKnocksToConsole = true;
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintWarning("Configuration file is corrupt! Creating new default configuration.");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new Configuration();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        #endregion

        #region Data Storage

        private StoredData _data;
        private bool _isDataDirty;
        private Timer _autoSaveTimer;

        private class StoredData
        {
            [JsonProperty(PropertyName = "TrackedDoors")]
            public Dictionary<ulong, ulong> TrackedDoors = new Dictionary<ulong, ulong>();
        }

        private void LoadData()
        {
            try
            {
                _data = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception ex)
            {
                PrintError($"Failed to load data file: {ex.Message}");
                _data = new StoredData();
            }

            if (_data?.TrackedDoors == null)
            {
                _data = new StoredData();
            }
        }

        private void SaveData()
        {
            if (_data == null) return;
            Interface.Oxide.DataFileSystem.WriteObject(Name, _data);
            _isDataDirty = false;
        }

        #endregion

        #region Active Event State

        private class ActiveKnockEvent
        {
            public ulong DoorNetId;
            public ulong DoorOwnerId;
            public Vector3 DoorPosition;
            public float KnockTime;
            public Timer ExpireTimer;
        }

        private ActiveKnockEvent _activeKnock;

        #endregion

        #region Helpers

        private bool IsOwnerOrTeammate(ulong ownerId, ulong claimantId, Door door = null)
        {
            if (ownerId == 0 || claimantId == 0) return false;
            if (ownerId == claimantId) return true;

            RelationshipManager.PlayerTeam team = RelationshipManager.ServerInstance?.FindPlayersTeam(ownerId);
            if (team != null && team.members.Contains(claimantId))
            {
                return true;
            }

            if (door != null)
            {
                BuildingPrivlidge priv = door.GetBuildingPrivilege();
                if (priv != null && priv.IsAuthed(claimantId))
                {
                    return true;
                }
            }

            return false;
        }

        private float GetNightModifier()
        {
            if (TOD_Sky.Instance == null || !TOD_Sky.Instance.IsNight) return 1f;

            float mod = 1f;
            try
            {
                if (SkillTree != null)
                {
                    object nightObj = SkillTree.Config["Night time settings"];
                    if (nightObj is Dictionary<string, object> dict && dict.TryGetValue("Modifier for xp gained at night [1.0 = standard]", out object val))
                    {
                        mod = Convert.ToSingle(val);
                    }
                }
            }
            catch {}

            if (mod <= 1f && _config.NightBonusMultiplier > 1f)
            {
                mod = _config.NightBonusMultiplier;
            }

            return mod;
        }

        private bool IsEventActive()
        {
            if (!_config.EnableDateGate) return true;

            DateTime now = DateTime.UtcNow;
            DateTime start;
            DateTime end;

            if (!DateTime.TryParse(_config.EventStartDate, out start))
            {
                start = new DateTime(now.Year, 10, 18, 0, 0, 0, DateTimeKind.Utc);
            }

            if (!DateTime.TryParse(_config.EventEndDate, out end))
            {
                end = new DateTime(now.Year, 11, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            return now >= start && now < end;
        }

        private bool CheckDateGate()
        {
            if (!IsEventActive())
            {
                Puts($"[GhostKnocker] Current date ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC) is outside the active event window (Oct 18 - Nov 1). Unloading plugin.");
                NextTick(() => Interface.Oxide.UnloadPlugin(Name));
                return false;
            }
            return true;
        }

        #endregion

        #region Initialization & Lifecycle

        private Timer _intervalTimer;

        private void Init()
        {
            permission.RegisterPermission(AdminPermission, this);
            cmd.AddChatCommand("ghostknock", this, nameof(CmdChatKnock));
            cmd.AddConsoleCommand("ghostknock", this, nameof(CmdConsoleKnock));
            LoadData();
        }

        private void OnServerInitialized()
        {
            if (!CheckDateGate()) return;

            if (_data.TrackedDoors.Count == 0)
            {
                PopulateExistingDoors();
            }
            else
            {
                Puts($"Loaded {_data.TrackedDoors.Count} tracked doors from data file.");
            }

            StartKnockTimer();

            _autoSaveTimer = timer.Every(_config.DataAutoSaveMinutes * 60f, () =>
            {
                if (_isDataDirty)
                {
                    SaveData();
                }
            });
        }

        private void Unload()
        {
            _intervalTimer?.Destroy();
            _autoSaveTimer?.Destroy();
            _activeKnock?.ExpireTimer?.Destroy();
            _activeKnock = null;

            if (_isDataDirty)
            {
                SaveData();
            }
        }

        private void PopulateExistingDoors()
        {
            int added = 0;
            foreach (BaseNetworkable ent in BaseNetworkable.serverEntities)
            {
                Door door = ent as Door;
                if (door != null && door.net != null && door.OwnerID != 0)
                {
                    _data.TrackedDoors[door.net.ID.Value] = door.OwnerID;
                    added++;
                }
            }

            if (added > 0)
            {
                _isDataDirty = true;
                SaveData();
                Puts($"Initial scan completed: indexed {added} player-owned doors into data file.");
            }
        }

        #endregion

        #region Entity Tracking

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            Door door = entity as Door;
            if (door == null || door.net == null || door.OwnerID == 0) return;

            ulong netId = door.net.ID.Value;
            if (!_data.TrackedDoors.ContainsKey(netId))
            {
                _data.TrackedDoors[netId] = door.OwnerID;
                _isDataDirty = true;
            }
        }

        private void OnEntityKill(BaseNetworkable entity)
        {
            Door door = entity as Door;
            if (door == null || door.net == null) return;

            ulong netId = door.net.ID.Value;
            if (_data.TrackedDoors.Remove(netId))
            {
                _isDataDirty = true;
            }

            if (_activeKnock != null && _activeKnock.DoorNetId == netId)
            {
                _activeKnock.ExpireTimer?.Destroy();
                _activeKnock = null;
            }
        }

        #endregion

        #region Knock Execution

        private void StartKnockTimer()
        {
            _intervalTimer?.Destroy();

            float delay = _config.UseRandomInterval
                ? UnityEngine.Random.Range(_config.MinIntervalSeconds, _config.MaxIntervalSeconds)
                : _config.KnockIntervalSeconds;

            _intervalTimer = timer.Once(delay, () =>
            {
                ExecuteRandomKnock();
                StartKnockTimer();
            });
        }

        private bool ExecuteRandomKnock()
        {
            if (!CheckDateGate()) return false;

            List<BasePlayer> candidatePlayers = Facepunch.Pool.Get<List<BasePlayer>>();
            List<Door> nearbyDoors = Facepunch.Pool.Get<List<Door>>();
            List<Door> validDoors = Facepunch.Pool.Get<List<Door>>();

            try
            {
                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (p != null && p.IsConnected && !p.IsDead() && !p.IsSleeping() && !p.IsSpectating())
                    {
                        candidatePlayers.Add(p);
                    }
                }

                if (candidatePlayers.Count == 0) return false;

                for (int i = candidatePlayers.Count - 1; i > 0; i--)
                {
                    int swapIndex = UnityEngine.Random.Range(0, i + 1);
                    BasePlayer temp = candidatePlayers[i];
                    candidatePlayers[i] = candidatePlayers[swapIndex];
                    candidatePlayers[swapIndex] = temp;
                }

                float searchRadius = _config.PlayerAudibleDistance;
                Door chosenDoor = null;
                ulong chosenOwnerId = 0;

                foreach (BasePlayer candidatePlayer in candidatePlayers)
                {
                    nearbyDoors.Clear();
                    validDoors.Clear();

                    Vis.Entities(candidatePlayer.transform.position, searchRadius, nearbyDoors, -1, QueryTriggerInteraction.Collide);

                    foreach (Door door in nearbyDoors)
                    {
                        if (door == null || door.IsDestroyed || door.net == null) continue;

                        if (_config.OnlyClosedDoors && door.IsOpen()) continue;

                        if (Vector3.Distance(candidatePlayer.transform.position, door.transform.position) > searchRadius) continue;

                        ulong doorOwner = door.OwnerID != 0 ? door.OwnerID : (_data.TrackedDoors.TryGetValue(door.net.ID.Value, out ulong tracked) ? tracked : 0);
                        if (doorOwner == 0)
                        {
                            BuildingPrivlidge priv = door.GetBuildingPrivilege();
                            if (priv != null && priv.OwnerID != 0)
                            {
                                doorOwner = priv.OwnerID;
                            }
                        }

                        if (doorOwner == 0) continue;

                        if (!IsOwnerOrTeammate(doorOwner, candidatePlayer.userID, door)) continue;

                        validDoors.Add(door);
                    }

                    if (validDoors.Count == 0)
                    {
                        continue;
                    }

                    chosenDoor = validDoors[UnityEngine.Random.Range(0, validDoors.Count)];
                    chosenOwnerId = chosenDoor.OwnerID != 0 ? chosenDoor.OwnerID : (_data.TrackedDoors.TryGetValue(chosenDoor.net.ID.Value, out ulong oId) ? oId : 0);
                    if (chosenOwnerId == 0)
                    {
                        BuildingPrivlidge priv = chosenDoor.GetBuildingPrivilege();
                        if (priv != null && priv.OwnerID != 0)
                        {
                            chosenOwnerId = priv.OwnerID;
                        }
                    }

                    if (chosenOwnerId != 0 && !_data.TrackedDoors.ContainsKey(chosenDoor.net.ID.Value))
                    {
                        _data.TrackedDoors[chosenDoor.net.ID.Value] = chosenOwnerId;
                        _isDataDirty = true;
                    }

                    break;
                }

                if (chosenDoor == null || chosenOwnerId == 0) return false;

                PerformSingleKnock(chosenDoor, chosenOwnerId);
                return true;
            }
            finally
            {
                Facepunch.Pool.FreeUnmanaged(ref candidatePlayers);
                Facepunch.Pool.FreeUnmanaged(ref nearbyDoors);
                Facepunch.Pool.FreeUnmanaged(ref validDoors);
            }
        }

        private void PerformSingleKnock(Door door, ulong ownerId)
        {
            if (door == null || door.IsDestroyed) return;

            if (door.OwnerID != 0 && door.OwnerID != ownerId)
            {
                ownerId = door.OwnerID;
                _data.TrackedDoors[door.net.ID.Value] = ownerId;
                _isDataDirty = true;
            }

            BasePlayer ownerPlayer = BasePlayer.FindByID(ownerId) ?? BasePlayer.FindSleeping(ownerId);

            BaseEntity slot = door.GetSlot(BaseEntity.Slot.LowerCenterDecoration);
            DoorKnocker knocker = slot?.GetComponent<DoorKnocker>();

            if (knocker != null)
            {
                knocker.ClientRPC(RpcTarget.NetworkGroup("ClientKnock"), door.CenterPoint());
            }

            string knockPrefab = (door.knockEffect != null && door.knockEffect.isValid)
                ? door.knockEffect.resourcePath
                : (door.ShortPrefabName.Contains("metal") || door.ShortPrefabName.Contains("armored")
                    ? "assets/prefabs/building/door.hinged/effects/door-metal-knock.prefab"
                    : "assets/prefabs/building/door.hinged/effects/door-wood-knock.prefab");

            Effect.server.Run(knockPrefab, door.CenterPoint());
            Interface.CallHook("OnDoorKnocked", door, ownerPlayer);

            if (_config.LogKnocksToConsole)
            {
                string ownerName = ownerPlayer != null ? ownerPlayer.displayName : ownerId.ToString();
                Puts($"[GhostKnock] Knocked on door {door.net.ID.Value} (Owner: {ownerName}) at {door.transform.position}");
            }

            if (_config.EnableEvent)
            {
                RegisterActiveEvent(door, ownerId);
            }
        }

        private void RegisterActiveEvent(Door door, ulong ownerId)
        {
            _activeKnock?.ExpireTimer?.Destroy();

            ulong netId = door.net.ID.Value;
            Vector3 doorPos = door.transform.position;

            _activeKnock = new ActiveKnockEvent
            {
                DoorNetId = netId,
                DoorOwnerId = ownerId,
                DoorPosition = doorPos,
                KnockTime = Time.realtimeSinceStartup,
                ExpireTimer = timer.Once(_config.EventTimeLimitSeconds, () =>
                {
                    if (_activeKnock != null && _activeKnock.DoorNetId == netId)
                    {
                        _activeKnock = null;
                    }
                })
            };

            if (_config.NotifyOnKnock)
            {
                string msg = _config.KnockAnnouncementMessage.Replace("{time}", _config.EventTimeLimitSeconds.ToString("0"));

                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (p == null || !p.IsConnected || p.IsSpectating()) continue;

                    if (Vector3.Distance(p.transform.position, doorPos) <= _config.PlayerAudibleDistance)
                    {
                        if (IsOwnerOrTeammate(ownerId, p.userID, door))
                        {
                            p.ChatMessage(msg);
                        }
                    }
                }
            }
        }

        #endregion

        #region Door Open & Reward Handling

        private void OnDoorOpened(Door door, BasePlayer player)
        {
            if (!_config.EnableEvent || _activeKnock == null) return;
            if (door == null || player == null || door.net == null) return;

            if (door.net.ID.Value == _activeKnock.DoorNetId)
            {
                if (!IsOwnerOrTeammate(_activeKnock.DoorOwnerId, player.userID, door))
                {
                    return;
                }

                float elapsed = Time.realtimeSinceStartup - _activeKnock.KnockTime;
                if (elapsed <= _config.EventTimeLimitSeconds)
                {
                    ulong ownerId = _activeKnock.DoorOwnerId;
                    Vector3 knockPos = _activeKnock.DoorPosition;

                    _activeKnock.ExpireTimer?.Destroy();
                    _activeKnock = null;

                    RewardPlayer(door, player, ownerId, knockPos, elapsed);
                }
                else
                {
                    _activeKnock.ExpireTimer?.Destroy();
                    _activeKnock = null;
                }
            }
        }

        private void RewardPlayer(Door door, BasePlayer player, ulong ownerId, Vector3 knockPos, float elapsedSeconds)
        {
            if (_config.PlayCelebrationEffect && !string.IsNullOrEmpty(_config.RewardEffectPrefab))
            {
                Effect.server.Run(_config.RewardEffectPrefab, door.CenterPoint());
            }

            bool gaveXP = false;
            double finalXP = _config.SkillTreeXPAmount;
            float decayPercent = 0f;

            if (_config.EnableXPDecay && elapsedSeconds > _config.XPDecayGraceSeconds)
            {
                float overtime = elapsedSeconds - _config.XPDecayGraceSeconds;
                decayPercent = Mathf.Clamp(overtime * _config.XPDecayPercentPerSecond, 0f, 100f);
                finalXP = Math.Max(_config.MinimumXPFloor, _config.SkillTreeXPAmount * (1.0 - (decayPercent / 100.0)));
                finalXP = Math.Round(finalXP, 0);
            }

            float nightMod = GetNightModifier();
            bool isNightBonus = nightMod > 1.001f;
            if (isNightBonus)
            {
                finalXP = Math.Round(finalXP * nightMod, 0);
            }

            if (SkillTree != null && finalXP > 0)
            {
                try
                {
                    SkillTree.Call("AwardXP", player, finalXP, "GhostKnocker", true);
                    gaveXP = true;
                }
                catch (Exception ex)
                {
                    PrintError($"Error awarding Skill Tree XP: {ex.Message}");
                }
            }

            player.ChatMessage(_config.RewardWinMessage);
            if (gaveXP)
            {
                string nightNotice = isNightBonus ? $" 🌙 <color=#88ccff>(+{((nightMod - 1f) * 100f):0}% Night Bonus!)</color>" : "";
                if (decayPercent > 0)
                {
                    player.ChatMessage($"<color=#ffa500>[Trick or Treat]</color> Answered in <b>{elapsedSeconds:0.0}s</b> (<color=#ff6666>-{decayPercent:0}% decay</color>){nightNotice}. Awarded <b>+{finalXP} Skill Tree XP</b>!");
                }
                else
                {
                    player.ChatMessage($"<color=#ffa500>[Trick or Treat]</color> ⚡ Answered in <b>{elapsedSeconds:0.0}s</b> (Full speed bonus!){nightNotice}. Awarded <b>+{finalXP} Skill Tree XP</b>!");
                }
            }

            if (!string.IsNullOrEmpty(_config.RangeClaimMessage))
            {
                string teamNightNotice = isNightBonus ? " 🌙 (Night Bonus!)" : "";
                string rangeMsg = _config.RangeClaimMessage
                    .Replace("{player}", player.displayName)
                    .Replace("{time}", elapsedSeconds.ToString("0.0"))
                    .Replace("{xp}", finalXP.ToString("0") + teamNightNotice);

                foreach (BasePlayer p in BasePlayer.activePlayerList)
                {
                    if (p == null || !p.IsConnected || p.IsSpectating() || p == player) continue;

                    if (Vector3.Distance(p.transform.position, knockPos) <= _config.PlayerAudibleDistance)
                    {
                        if (IsOwnerOrTeammate(ownerId, p.userID, door))
                        {
                            p.ChatMessage(rangeMsg);
                        }
                    }
                }
            }

            if (_config.BroadcastRewardToChat)
            {
                rust.BroadcastChat("<color=#ffa500>[Ghost Event]</color>", $"🎃 <b>{player.displayName}</b> answered the ghost's door in time and claimed the prize!");
            }

            if (_config.LogKnocksToConsole)
            {
                Puts($"[GhostKnock] Player {player.displayName} ({player.UserIDString}) opened the door in {elapsedSeconds:0.0}s and received {finalXP} XP (-{decayPercent:0}% decay).");
            }
        }

        #endregion

        #region Admin Commands

        [ChatCommand("knock")]
        private void CmdChatKnock(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, AdminPermission))
            {
                player.ChatMessage("<color=#ff4444>You do not have permission to use this command.</color>");
                return;
            }

            HandleAdminKnock(player, args);
        }

        [ConsoleCommand("knock")]
        private void CmdConsoleKnock(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null && !permission.UserHasPermission(arg.Player().UserIDString, AdminPermission))
            {
                arg.ReplyWith("You do not have permission to use this command.");
                return;
            }

            if (arg.HasArgs() && string.Equals(arg.GetString(0), "count", StringComparison.OrdinalIgnoreCase))
            {
                arg.ReplyWith($"GhostKnocker is tracking {_data.TrackedDoors.Count} doors in the data file.");
                return;
            }

            bool success = ExecuteRandomKnock();
            arg.ReplyWith(success ? "Ghost knock triggered successfully!" : "Could not find a suitable candidate door to knock on.");
        }

        private void HandleAdminKnock(BasePlayer player, string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "count", StringComparison.OrdinalIgnoreCase))
            {
                player.ChatMessage($"<color=#ffa500>[GhostKnocker]</color> Currently tracking <b>{_data.TrackedDoors.Count}</b> doors.");
                return;
            }

            bool executed = ExecuteRandomKnock();
            if (executed)
            {
                player.ChatMessage("<color=#ffa500>[GhostKnocker]</color> Spooky ghost knock triggered on a random player door!");
            }
            else
            {
                player.ChatMessage("<color=#ff4444>[GhostKnocker]</color> No valid candidate door found (check if any players are online/nearby).");
            }
        }

        #endregion
    }
}
