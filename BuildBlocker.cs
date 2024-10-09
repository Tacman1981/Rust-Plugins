//Written so that permissions block placement of the specific deployable when granted. There are just too many permissions to grant for placement
//If you want it to be permissions for placement, change the if permission.UserHasPermission to include ! after the bracket but before permission inside CanBuild
//Will rewrite soon, I know its a bit long but it works as intended.
//Good example of a self populating, permissions creating config file
using System;
using System.Collections.Generic;
using System.IO;
using Oxide.Core.Plugins;
using Newtonsoft.Json;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Build Blocker", "Tacman", "1.0.0")]
    [Description("Prevent building of specified entities by permission")]
    public class BuildBlocker : RustPlugin
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

        //We create the config template here, with only a permissions dictionary.
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

        //Create an empty permissions template
        protected override void LoadDefaultConfig()
        {
            config = Configuration.DefaultConfig();

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            try
            {
                Config.WriteObject(config, true);
                Puts("Configuration saved successfully.");
            }
            catch (Exception ex)
            {
                PrintWarning($"Error saving configuration: {ex.Message}");
            }
        }
        
        //Ensure that the config is not broken before proceeding to use it.
        private void ValidateConfig()
        {
            if (config.Permissions == null)
            {
                config.Permissions = new Dictionary<string, string>();
            }
        }

        //Creating the permissions here from the specified categories, if the items are deployable.
        private void PopulateConfig()
        {
            if (ItemManager.itemList == null || ItemManager.itemList.Count == 0)
            {
                Puts("ItemManager.itemList is null or empty. Cannot populate config.");
                return;
            }

            var newPermissions = new Dictionary<string, string>();
            bool hasNewPermissions = false;
            //foreach loop to find the items to register permissions with
            foreach (var itemDefinition in ItemManager.itemList)
            {
                if (IsBlacklisted(itemDefinition.shortname))
                {
                    //Puts($"Skipping blacklisted item: {itemDefinition.shortname}");
                    continue;
                }

                if (IsDeployable(itemDefinition))
                {
                    string shortName = itemDefinition.shortname;
                    string prefabName = SanitizeDisplayName(itemDefinition.displayName?.english ?? shortName);
                    string permissionName = $"buildblocker.{prefabName}";

                    if (!config.Permissions.ContainsKey(shortName))
                    {
                        newPermissions[shortName] = permissionName;
                        hasNewPermissions = true;
                        Puts($"Added new permission for item '{shortName}' with permission '{permissionName}'.");
                    }
                }
                else
                {
                    //Do nothing here, added this to see if deployable check was working correctly
                    //Puts($"Item '{itemDefinition.shortname}' is not deployable.");
                }
            }

            if (hasNewPermissions)
            {
                foreach (var kvp in newPermissions)
                {
                    config.Permissions[kvp.Key] = kvp.Value;
                }
                SaveConfig();
                Puts("Config updated with new deployable item permissions.");
            }
            else
            {
                Puts("No new permissions to add.");
            }
        }
        
        //Hardcoded blacklist. This is done to prevent the config becoming too cluttered.
        private bool IsBlacklisted(string shortName)
        {
            //Blacklisted Items (hardcoded to not clutter config)
            var blacklist = new HashSet<string>
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
                "rf_pager",
                "mobilephone",
                "fun.bass",
                "fun.cowbell",
                "fun.flute",
                "fun.guitar",
                "fun.trumpet",
                "fun.tuba",
                "fun.jerrycanguitar",
                "wrappedgift",
                "wrappingpaper",
                "fun.casetterecorder",
                "cassette.medium",
                "cassette",
                "cassette.short",
                "building.planner",
                "fun.boomboxportable",
                "botabag",
                "carvable.pumpkin",
                "map"
            };

            return blacklist.Contains(shortName);
        }

        //checking for common categories with deployables in them
        private bool IsDeployable(ItemDefinition itemDefinition)
        {
            return itemDefinition.category == ItemCategory.Construction || itemDefinition.category == ItemCategory.Electrical || itemDefinition.category == ItemCategory.Fun || itemDefinition.category == ItemCategory.Items || itemDefinition.category == ItemCategory.Traps || itemDefinition.category == ItemCategory.Common || itemDefinition.category == ItemCategory.Misc;
        }

        //here I clean up the display names of the entities, so they can be used as easy to find permissions
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
                    //Register the permissions, to allow customizing of entity blocking with permissions
                    permission.RegisterPermission(permissionName, this);
                    //Puts($"Registered permission: {permissionName}");
                }
            }

            Puts("Permissions registration complete. Granted means disallowed placement");
        }

        #endregion

        #region Hooks
        //Check if players have the blocking permission to prevent building.
        private object CanBuild(Planner planner, Construction prefab)
        {
            if (prefab == null || planner == null)
            {
                PrintWarning("Planner or prefab is null.");
                return null;
            }
            //Getting player ID to check for permission.
            var player = planner.GetOwnerPlayer();
            if (player == null)
            {
                Puts("Player is null.");
                return null;
            }

            //Get the prefab name
            string prefabShortName = prefab.fullName;

            //Check if the entity is in the permissions dictionary
            foreach (var kvp in config.Permissions)
            {
                if (prefabShortName.Contains(kvp.Key))
                {
                    string permissionName = kvp.Value;

                    if (permission.UserHasPermission(player.UserIDString, permissionName))
                    {
                        //Notify player they are not allowed to place this object(permission granted)
                        player.ChatMessage(lang.GetMessage("NotAllowed", this));

                        // Return false to prevent the build
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
                    ["NotAllowed"] = "You are not permitted to place this entity."
                },
                ["fr"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Vous n'êtes pas autorisé à placer cette entité."
                },
                ["de"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Sie sind nicht berechtigt, dieses Objekt zu platzieren."
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "No tienes permiso para colocar esta entidad."
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
                        Puts($"Created/Updated language file: {filePath}");
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
