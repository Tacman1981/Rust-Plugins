using System;
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Custom Blue Berry Gather", "Tacman", "0.0.3")]
    [Description("Blueberries when growing blue berries")]
    internal class CustomBlueBerryGather : RustPlugin
    {
        private int minBonusAmount;
        private int maxBonusAmount;
        private double bonusChance;
        private string permName = "customblueberrygather.bonus";

        protected override void LoadDefaultConfig()
        {
            // Set default values for config options
            Config["MinBonusAmount"] = minBonusAmount = GetConfig("MinBonusAmount", 1);
            Config["MaxBonusAmount"] = maxBonusAmount = GetConfig("MaxBonusAmount", 4);
            Config["BonusChance"] = bonusChance = GetConfig("BonusChance", 0.5);
            SaveConfig();
        }

        private T GetConfig<T>(string name, T defaultValue)
        {
            if (Config[name] == null)
            {
                Config[name] = defaultValue;
                return defaultValue;
            }
            return (T)Convert.ChangeType(Config[name], typeof(T));
        }

        void Init()
        {
            LoadDefaultConfig();
            permission.RegisterPermission(permName, this);
        }

        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, permName))
            {
                // Check if the gathered item is a berry
                if (item.info.shortname == "blue.berry")
                {
                    // Generate a random number between 0 and 1
                    double randomValue = UnityEngine.Random.value;

                    // Check if the random value is less than or equal to the bonus chance
                    if (randomValue <= bonusChance)
                    {
                        // Generate a random number between the min and max bonus amount
                        int bonusAmount = UnityEngine.Random.Range(minBonusAmount, maxBonusAmount + 1);

                        // Create the custom item (e.g., black raspberries)
                        Item customItem = ItemManager.CreateByName("bluebrries", bonusAmount);

                        // Give the custom item to the player's inventory
                        player.GiveItem(customItem, BaseEntity.GiveItemReason.PickedUp);
                    }
                }
            }
        }
    }
}
