using System;
using System.Collections.Generic;
using System.IO;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Can Build This?", "Tacman", "1.0.0")]
    [Description("Automatically populates the configuration file with item permissions from the ItemManager.")]
    public class CanBuildThis : RustPlugin
    {
        #region Config

        private static Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Permissions")]
            public Dictionary<string, string> Permissions { get; set; } = new Dictionary<string, string>();

            [JsonProperty(PropertyName = "Blacklist")]
            public List<string> Blacklist { get; set; } = new List<string>();

            [JsonProperty(PropertyName = "ALLPermission")]
            public string ALLPermission { get; set; } = "canbuildthis.all";

            public static Configuration DefaultConfig()
            {
                return new Configuration();
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>() ?? Configuration.DefaultConfig();
                ValidateConfig();
                PopulateConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Error loading configuration. Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.DefaultConfig();

            // Predefined blacklist items
            config.Blacklist.AddRange(new List<string>
            {
                "wiretool",
                "pipetool",
                "hosetool",
                "attackhelicopter",
                "motorbike",
                "motorbike_sidecar",
                "bicycle",
                "trike",
                "kayak",
                "rhib",
                "rowboat",
                "tugboat",
                "minihelicopter.repair",
                "mlrs",
                "scraptransportheli.repair",
                "snowmobile",
                "snowmobiletomaha",
                "submarineduo",
                "submarinesolo",
                "locomotive",
                "wagon",
                "workcart",
                "rf_pager"
            });

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            try
            {
                Config.WriteObject(config, true);
                PrintToConsole("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                PrintWarning($"Error saving configuration: {ex.Message}");
            }
        }

        private void ValidateConfig()
        {
            if (config.Permissions == null)
            {
                config.Permissions = new Dictionary<string, string>();
            }

            if (config.Blacklist == null)
            {
                config.Blacklist = new List<string>();
            }
        }

        private void PopulateConfig()
        {
            var newPermissions = new Dictionary<string, string>();

            try
            {
                if (ItemManager.itemList != null)
                {
                    foreach (var itemDefinition in ItemManager.itemList)
                    {
                        if (config.Blacklist.Contains(itemDefinition.shortname))
                        {
                            continue;
                        }

                        if (IsDeployable(itemDefinition))
                        {
                            string shortName = itemDefinition.shortname;
                            string prefabName = SanitizeDisplayName(itemDefinition.displayName?.english ?? shortName);
                            string permissionName = $"canbuildthis.{prefabName}";

                            if (!newPermissions.ContainsKey(shortName))
                            {
                                newPermissions[shortName] = permissionName;
                                PrintToConsole($"Added permission for item '{shortName}' with permission '{permissionName}'.");
                            }
                        }
                    }

                    config.Permissions = newPermissions;
                    SaveConfig();
                    PrintToConsole("Config updated with deployable item permissions.");
                }
                else
                {
                    PrintWarning("ItemManager.itemList is null. Cannot populate config.");
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Error populating config: {ex.Message}");
            }
        }

        private bool IsDeployable(ItemDefinition itemDefinition)
        {
            // Adjust this to use the actual method or property that checks deployability
            // This is a placeholder example; replace with the actual check if different
            return itemDefinition.GetDeployable() != null;
        }

        private string SanitizeDisplayName(string displayName)
        {
            return string.IsNullOrEmpty(displayName) ? string.Empty : displayName.Replace(' ', '_').Replace('/', '_').ToLower();
        }

        #endregion

        #region Initialization

        private Timer _initTimer;

        private void Init()
        {
            _initTimer = timer.Every(5f, () =>
            {
                if (ItemManager.itemList != null)
                {
                    LoadConfig();
                    RegisterPermissions();
                    EnsureLanguageFiles();
                    _initTimer?.Destroy();
                }
                else
                {
                    PrintWarning("ItemManager.itemList is still null. Retrying...");
                }
            });
        }

        #endregion

        #region Permissions

        private void RegisterPermissions()
        {
            foreach (var permissionName in config.Permissions.Values)
            {
                if (!permission.PermissionExists(permissionName))
                {
                    permission.RegisterPermission(permissionName, this);
                    PrintToConsole($"Registered permission: {permissionName}");
                }
            }

            // Register the ALL permission
            if (!permission.PermissionExists(config.ALLPermission))
            {
                permission.RegisterPermission(config.ALLPermission, this);
                PrintToConsole($"Registered permission: {config.ALLPermission}");
            }

            PrintToConsole("Permissions registration complete.");
        }

        #endregion

        #region Hooks

        private object CanBuild(Planner planner, Construction prefab)
        {
            if (prefab == null || planner == null)
            {
                PrintWarning("Planner or prefab is null.");
                return null;
            }

            var player = planner.GetOwnerPlayer();
            if (player == null)
            {
                PrintWarning("Player is null.");
                return null;
            }

            // Check for ALL permission first
            if (permission.UserHasPermission(player.UserIDString, config.ALLPermission))
            {
                return null; // Player has permission to build anything
            }

            string prefabName = prefab.fullName;

            foreach (var kvp in config.Permissions)
            {
                if (prefabName.Contains(kvp.Key))
                {
                    string permissionName = kvp.Value;

                    if (!permission.UserHasPermission(player.UserIDString, permissionName))
                    {
                        player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
                        return false;
                    }

                    return null;
                }
            }

            return null;
        }

        #endregion

        #region Language

        private void EnsureLanguageFiles()
        {
            string baseLangDir = Path.Combine(Directory.GetCurrentDirectory(), "oxide", "lang");
            var languages = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["NoPermission"] = "You are not permitted to place this entity."
                },
                ["fr"] = new Dictionary<string, string>
                {
                    ["NoPermission"] = "Vous n'êtes pas autorisé à placer cette entité."
                },
                ["de"] = new Dictionary<string, string>
                {
                    ["NoPermission"] = "Sie sind nicht berechtigt, dieses Objekt zu platzieren."
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["NoPermission"] = "No tienes permiso para colocar esta entidad."
                }
            };

            foreach (var lang in languages)
            {
                string langDir = Path.Combine(baseLangDir, lang.Key);
                if (!Directory.Exists(langDir))
                {
                    Directory.CreateDirectory(langDir);
                }

                string filePath = Path.Combine(langDir, $"{Name}.json");
                try
                {
                    string json = JsonConvert.SerializeObject(lang.Value, Formatting.Indented);

                    if (!File.Exists(filePath) || File.ReadAllText(filePath) != json)
                    {
                        File.WriteAllText(filePath, json);
                        PrintToConsole($"Created/Updated language file: {filePath}");
                    }
                }
                catch (Exception ex)
                {
                    PrintWarning($"Error creating/updating language file {filePath}: {ex.Message}");
                }
            }

            foreach (var langCode in languages.Keys)
            {
                string filePath = Path.Combine(baseLangDir, langCode, $"{Name}.json");
                var messages = ReadLanguageFile(filePath);
                lang.RegisterMessages(messages, this, langCode);
            }
        }

        private Dictionary<string, string> ReadLanguageFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                }
            }
            catch (Exception ex)
            {
                PrintWarning($"Error reading language file {filePath}: {ex.Message}");
            }
            return new Dictionary<string, string>();
        }

        #endregion
    }
}
