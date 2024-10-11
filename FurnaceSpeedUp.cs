//This will increase furnaces speed the more it is used. Config needs some work and logic too but it currently functions as is. Perfect for lightly modded pvp.
//Example of time as a currency.

using Oxide.Core;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("FurnaceSpeedUp", "Tacman", "1.0.0")]
    [Description("Increases furnace smelting speed over time.")]
    public class FurnaceSpeedUp : RustPlugin
    {
        private const int MaxSpeed = 100;
        private Dictionary<ulong, int> furnaceCycleCount = new Dictionary<ulong, int>();
        private Dictionary<ulong, float> furnaceSpeeds = new Dictionary<ulong, float>();
        private Dictionary<ulong, int> furnaceSpeedUpgrades = new Dictionary<ulong, int>(); // New dictionary for speed upgrades
        private List<string> furnaceNames;

        public class FurnaceData
        {
            public float Speed { get; set; }
            public int Cycles { get; set; }
            public int Upgrades { get; set; }

            public FurnaceData(int speed, int cycles, int upgrades)
            {
                Speed = speed;
                Cycles = cycles;
                Upgrades = upgrades;
            }
        }


        private void LoadDefaultConfig()
        {
            // Default configuration
            Config["FurnaceNames"] = new List<string>
            {
                "furnace", "furnace.large" // Include large furnace by default
            };

            // Add other default values
            Config["DailyUpgradeLimit"] = 10; // Example for daily upgrade limit

            SaveConfig(); // Save the defaults to the config file
        }

        private void LoadConfig()
        {
            // Attempt to read the config value for furnace names
            furnaceNames = Config.Get<List<string>>("FurnaceNames");

            // If furnaceNames is null or empty, initialize it with default
            if (furnaceNames == null || furnaceNames.Count == 0)
            {
                furnaceNames = new List<string> { "furnace", "furnace.large" }; // Default value
                Config["FurnaceNames"] = furnaceNames; // Save it back to config
                SaveConfig(); // Save the config again to ensure it's written
            }

            // Load daily upgrade limit
            int dailyUpgradeLimit = Config.Get<int>("DailyUpgradeLimit");
            // Use the dailyUpgradeLimit variable as needed in your plugin
        }

        private void Init()
        {
            // Check if config exists; if not, load defaults
            if (!Config.Exists())
            {
                LoadDefaultConfig(); // Load defaults if config does not exist
            }

            LoadConfig(); // Load the configuration

            // Load previously saved furnace speeds from data file
            LoadFurnaceSpeeds();
            Subscribe(nameof(OnPickup));
        }

        private void OnPickup(Item item, BasePlayer player)
        {
            if (furnaceNames.Contains(item.info.shortname))
            {
                BaseOven oven = item.GetWorldEntity() as BaseOven;
                if (oven != null && oven.ShortPrefabName.Equals("Furnace"))
                {
                    ulong ovenId = oven.net.ID.Value;
                    float currentSpeed = oven.smeltSpeed;
                    int cycles = furnaceCycleCount.ContainsKey(ovenId) ? furnaceCycleCount[ovenId] : 0;
                    int upgrades = furnaceSpeedUpgrades.ContainsKey(ovenId) ? furnaceSpeedUpgrades[ovenId] : 0;

                    // Store the data in the furnaceSpeeds dictionary
                    furnaceSpeeds[ovenId] = currentSpeed;
                    furnaceCycleCount[ovenId] = cycles;
                    furnaceSpeedUpgrades[ovenId] = upgrades;

                    // Rename the oven for identification
                    string newName = $"{oven.ShortPrefabName}_Speed{currentSpeed}_Cycles{cycles}_Upgrades{upgrades}";
                    UpdateOvenName(oven, furnaceSpeedUpgrades[ovenId] + 1);

                    Puts($"Furnace {ovenId} renamed to {newName} upon pickup.");
                }
                else
                {
                    Puts("Could not retrieve the BaseOven from the picked-up item.");
                }
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is BaseOven oven)
            {
                ulong ovenId = oven.net.ID.Value;

                // Check if we have saved data for this oven
                if (furnaceSpeeds.TryGetValue(ovenId, out float speed))
                {
                    oven.smeltSpeed = (int)speed;
                    Puts($"Furnace {ovenId} has been spawned. Restored speed to {speed}.");
                }

                if (furnaceCycleCount.TryGetValue(ovenId, out int cycles))
                {
                    furnaceCycleCount[ovenId] = cycles; // Update cycle count if needed
                }

                if (furnaceSpeedUpgrades.TryGetValue(ovenId, out int upgrades))
                {
                    furnaceSpeedUpgrades[ovenId] = upgrades; // Update upgrades count if needed
                }
            }
        }

        // Override the OnOvenCooked method to listen to the event
        private void OnOvenCooked(BaseOven oven)
        {
            ulong ovenId = oven.net.ID.Value;

            // Retrieve the cooked item from the oven
            var item = oven.inventory?.itemList.FirstOrDefault();

            if (!furnaceCycleCount.ContainsKey(ovenId))
            {
                furnaceCycleCount[ovenId] = 0;
                furnaceSpeedUpgrades[ovenId] = 0; // Initialize speed upgrades for new ovens
            }

            furnaceCycleCount[ovenId]++;

            int upgradeThreshold = oven.ShortPrefabName.Contains("large") ? 250 : 100;

            if (furnaceCycleCount[ovenId] % upgradeThreshold == 0)
            {
                int currentSpeed = GetCurrentFurnaceSpeed(ovenId);

                if (currentSpeed < MaxSpeed) // Check if current speed is below the max limit
                {
                    int newSpeed = currentSpeed + 1;
                    SetFurnaceSpeed(ovenId, newSpeed);
                    oven.smeltSpeed = newSpeed;
                    Puts($"Furnace {ovenId} speed increased to {newSpeed}");
                    furnaceCycleCount[ovenId] = 0;

                    furnaceSpeedUpgrades[ovenId]++; // Increment speed upgrades
                }
                else
                {
                    Puts($"Furnace {ovenId} has reached the maximum speed of {MaxSpeed}");
                }
            }
        }

        private void UpdateOvenName(BaseOven oven, int speedUpgradeCount)
        {
            string newName = $"{oven.ShortPrefabName} - Speed Upgrades: {speedUpgradeCount}";
            oven.gameObject.name = newName; // Update the name of the oven
            Puts($"Oven renamed to: {newName}");
        }

        private int GetCurrentFurnaceSpeed(ulong ovenId)
        {
            // Return the current speed or a default value if not set
            return furnaceSpeeds.TryGetValue(ovenId, out float speed) ? (int)speed : 1; // Default speed of 1 (adjust if needed)
        }

        private void SetFurnaceSpeed(ulong ovenId, float newSpeed)
        {
            // Update the stored speed for the furnace
            furnaceSpeeds[ovenId] = newSpeed;

            // Save the current speeds to a data file
            SaveFurnaceSpeeds();
        }

        private void LoadFurnaceSpeeds()
        {
            string filePath = $"{Interface.Oxide.DataDirectory}/FurnaceSpeeds.json";

            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    furnaceSpeeds = JsonConvert.DeserializeObject<Dictionary<ulong, float>>(json);
                    Puts("Furnace Data Loaded");
                }
                catch (Exception ex)
                {
                    Puts($"Failed to load furnace speeds: {ex.Message}");
                }
            }
        }

        private void SaveFurnaceSpeeds()
        {
            string filePath = $"{Interface.Oxide.DataDirectory}/FurnaceSpeeds.json";
            try
            {
                string json = JsonConvert.SerializeObject(furnaceSpeeds, Formatting.Indented);
                System.IO.File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Puts($"Failed to save furnace speeds: {ex.Message}");
            }
        }

        private void Unload()
        {
            // Save the current speeds before resetting
            SaveFurnaceSpeeds();

            // Reset all furnaces' speeds
            foreach (BaseOven oven in UnityEngine.Object.FindObjectsOfType<BaseOven>())
            {
                ResetFurnaceSpeed(oven);
            }
        }

        // Helper method to reset the furnace speed
        private void ResetFurnaceSpeed(BaseOven oven)
        {
            ulong ovenId = oven.net.ID.Value;

            // Reset the speed to default (1) or whatever your default is
            oven.smeltSpeed = 1; // Adjust this as necessary

            // Optionally, you might want to remove the oven from the stored speeds
            if (furnaceSpeeds.ContainsKey(ovenId))
            {
                Puts($"Furnace {ovenId} speed reset to default due to unload.");
            }
        }


        object OnOvenToggle(BaseOven oven, BasePlayer player)
        {
            ulong ovenId = oven.net.ID.Value;

            // Check if we have saved data for this oven
            if (furnaceSpeeds.TryGetValue(ovenId, out float savedSpeed))
            {
                // Apply the saved smelt speed to the furnace
                oven.smeltSpeed = (int)savedSpeed;
                Puts($"Furnace {ovenId} smelt speed set to {savedSpeed} after toggle.");
            }
            else
            {
                // If no saved speed, set it to default (1)
                oven.smeltSpeed = 1;
                Puts($"Furnace {ovenId} has no saved speed, set to default 1.");
            }

            // Check and apply the cycle count if available
            if (furnaceCycleCount.TryGetValue(ovenId, out int savedCycles))
            {
                furnaceCycleCount[ovenId] = savedCycles;
                Puts($"Furnace {ovenId} cycle count restored to {savedCycles}.");
            }

            // Check and apply the upgrade count if available
            if (furnaceSpeedUpgrades.TryGetValue(ovenId, out int savedUpgrades))
            {
                furnaceSpeedUpgrades[ovenId] = savedUpgrades;
                Puts($"Furnace {ovenId} upgrade count restored to {savedUpgrades}.");
            }

            return null;
        }

        private void OnNewSave()
        {
            string filePath = $"{Interface.Oxide.DataDirectory}/FurnaceSpeeds.json";
            System.IO.File.Delete(filePath);
            Puts("New save detected! Clearing data file.");
        }

        private void OnServerSave()
        {
            Puts("Saving Furnace Data");
            SaveFurnaceSpeeds();
        }
    }
}
