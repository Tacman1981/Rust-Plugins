using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("CustomComposting", "Tacman", "1.0.1")]
    [Description("Custom composter settings with configurable compostable items and values.")]
    public class CustomComposting : RustPlugin
    {
        private Dictionary<string, float> compostableItems = new();
        private ItemDefinition fertilizerDef;
        private bool processMultipleStacks;

        private void Init()
        {
            LoadConfigValues();
            fertilizerDef = ItemManager.FindItemDefinition("fertilizer");
            if (fertilizerDef == null)
            {
                PrintError("Failed to find the fertilizer item definition. Make sure the item name is correct.");
            }
            AddCovalenceCommand("fert", "FertCommand");
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Creating a new configuration file.");

            // Define default config settings
            Config["ProcessMultipleStacks"] = false;
            Config["CompostableItems"] = new Dictionary<string, float>
            {
                { "wood", 0.5f },
                { "stones", 1.0f }
            };

            // Save config
            SaveConfig();
        }

        private void LoadConfigValues()
        {
            try
            {
                // Load ProcessMultipleStacks setting
                if (Config["ProcessMultipleStacks"] is bool configProcessMultipleStacks)
                {
                    processMultipleStacks = configProcessMultipleStacks;
                }
                else
                {
                    processMultipleStacks = false;
                    Config["ProcessMultipleStacks"] = processMultipleStacks;
                    SaveConfig();
                }

                // Load CompostableItems dictionary
                var compostableItemsConfig = Config["CompostableItems"] as Dictionary<string, object>;
                if (compostableItemsConfig == null)
                {
                    PrintWarning("CompostableItems configuration is missing or invalid. Loading default values.");
                    LoadDefaultConfig();
                    compostableItemsConfig = Config["CompostableItems"] as Dictionary<string, object>;
                }

                compostableItems = JsonConvert.DeserializeObject<Dictionary<string, float>>(JsonConvert.SerializeObject(compostableItemsConfig))
                                   ?? new Dictionary<string, float>();
            }
            catch (JsonException ex)
            {
                PrintError($"Failed to deserialize CompostableItems: {ex.Message}. Loading default values.");
                LoadDefaultConfig();
                processMultipleStacks = Config["ProcessMultipleStacks"] is bool configProcessMultipleStacksAfterError ? configProcessMultipleStacksAfterError : false;
                var compostableItemsConfig = Config["CompostableItems"] as Dictionary<string, object>;
                compostableItems = JsonConvert.DeserializeObject<Dictionary<string, float>>(JsonConvert.SerializeObject(compostableItemsConfig))
                                   ?? new Dictionary<string, float>();
            }

            if (compostableItems.Count == 0)
            {
                PrintWarning("CompostableItems configuration is empty after loading. Loading default values.");
                LoadDefaultConfig();
                var compostableItemsConfig = Config["CompostableItems"] as Dictionary<string, object>;
                compostableItems = JsonConvert.DeserializeObject<Dictionary<string, float>>(JsonConvert.SerializeObject(compostableItemsConfig))
                                   ?? new Dictionary<string, float>();
            }
        }

        private void FertCommand(IPlayer player, string command, string[] args)
        {
            var basePlayer = player.Object as BasePlayer;
            if (basePlayer == null) return;

            int totalFertilizer = 0;

            foreach (var compostableItem in compostableItems)
            {
                List<Item> itemsToRemove = new List<Item>();
                int itemCount = 0;
                if (processMultipleStacks)
                {
                    foreach (var item in basePlayer.inventory.AllItems())
                    {
                        if (item.info.shortname == compostableItem.Key)
                        {
                            itemCount += item.amount;
                            itemsToRemove.Add(item);
                        }
                    }
                }
                else
                {
                    var item = basePlayer.inventory.FindItemByItemName(compostableItem.Key);
                    if (item != null)
                    {
                        itemCount = item.amount;
                        itemsToRemove.Add(item);
                    }
                }

                if (itemCount > 0)
                {
                    float compostValue = compostableItem.Value;
                    totalFertilizer += Mathf.FloorToInt(itemCount * compostValue);
                    foreach (var item in itemsToRemove)
                    {
                        item.RemoveFromContainer();
                        item.Remove();
                    }
                }
            }

            if (totalFertilizer > 0)
            {
                Item fertilizerItem = ItemManager.Create(fertilizerDef, totalFertilizer, 0UL);
                if (!fertilizerItem.MoveToContainer(basePlayer.inventory.containerMain, -1, true))
                {
                    fertilizerItem.Drop(basePlayer.transform.position, basePlayer.transform.forward, Quaternion.identity);
                }
                player.Reply($"You have received {totalFertilizer} fertilizer.");
            }
            else
            {
                player.Reply("You do not have any compostable items.");
            }
        }
    }
}
