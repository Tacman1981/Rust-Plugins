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
                PopulateConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            config = Configuration.DefaultConfig();
            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void PopulateConfig()
        {
            var newPermissions = new Dictionary<string, string>();

            if (ItemManager.itemList != null)
            {
                foreach (var itemDefinition in ItemManager.itemList)
                {
                    if (IsInCategory(itemDefinition, ItemCategory.Construction) ||
                        IsInCategory(itemDefinition, ItemCategory.Traps) ||
                        IsInCategory(itemDefinition, ItemCategory.Items))
                    {
                        // Use shortname for key and sanitized displayName for permission
                        string shortName = itemDefinition.shortname;
                        string prefabName = SanitizeDisplayName(itemDefinition.displayName?.english ?? shortName);

                        string permissionName = $"canbuildthis.{prefabName}";

                        // Use shortName as the key and permissionName as the value
                        if (!newPermissions.ContainsKey(shortName))
                        {
                            newPermissions[shortName] = permissionName;
                        }
                    }
                }
                config.Permissions = newPermissions;
                SaveConfig();
                PrintToConsole("Config updated with relevant item category permissions.");
            }
            else
            {
                PrintWarning("ItemManager.itemList is null. Cannot populate config.");
            }
        }

        private string SanitizeDisplayName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName))
            {
                return string.Empty;
            }

            // Replace spaces with underscores and convert to lowercase
            return displayName.Replace(' ', '_').ToLower();
        }

        private bool IsInCategory(ItemDefinition itemDefinition, ItemCategory category)
        {
            return itemDefinition.category == category;
        }

        #endregion

        #region Initialization

        private void Init()
        {
            timer.Once(1f, () =>
            {
                PrintToConsole("Init method called.");
                if (ItemManager.itemList == null)
                {
                    PrintWarning("ItemManager.itemList is still null. Initialization may be incomplete.");
                    return;
                }

                LoadConfig();
                RegisterPermissions();
                EnsureLanguageFiles();
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
                else
                {
                    PrintToConsole($"Permission already exists: {permissionName}");
                }
            }
            PrintToConsole("Permissions registration complete.");
        }

        #endregion

        #region Hooks

        private object CanBuild(Planner planner, Construction prefab)
        {
            if (prefab == null || planner == null)
            {
                PrintToConsole("CanBuild called with null prefab or planner.");
                return null;
            }

            string prefabName = prefab.fullName;

            var player = planner.GetOwnerPlayer();
            PrintToConsole($"CanBuild called for prefab: {prefabName} by player: {player?.displayName}");

            foreach (var kvp in config.Permissions)
            {
                if (prefabName.Contains(kvp.Key))
                {
                    if (player == null)
                    {
                        PrintToConsole("Player not found.");
                        return null;
                    }

                    string permissionName = kvp.Value;

                    if (!permission.UserHasPermission(player.UserIDString, permissionName))
                    {
                        player.ChatMessage(lang.GetMessage("NoPermission", this, player.UserIDString));
                        PrintToConsole($"Player {player.displayName} tried to build {prefabName} but lacks permission ({permissionName}).");
                        return false;
                    }

                    PrintToConsole($"Player {player.displayName} has permission to build {prefabName} ({permissionName}).");
                    return null;
                }
            }

            PrintToConsole($"No permission entry found for {prefabName}. Building allowed by default.");
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
                    File.WriteAllText(filePath, json);
                    PrintToConsole($"Created/Updated language file: {filePath}");
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
