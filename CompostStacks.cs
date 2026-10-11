using System.Collections.Generic;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "3.0.0")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private const string permissionName = "compoststacks.use";
        private ItemDefinition defaultFertilizerDef;
        private readonly Dictionary<int, float> compostableCache = new Dictionary<int, float>();

        private void Init()
        {
            permission.RegisterPermission(permissionName, this);
        }

        private void OnServerInitialized()
        {
            defaultFertilizerDef = ItemManager.FindItemDefinition("fertilizer");
            foreach (ItemDefinition def in ItemManager.itemList)
            {
                if (def == null) continue;
                ItemModCompostable compostable = def.GetComponent<ItemModCompostable>();
                if (compostable != null && compostable.TotalFertilizerProduced > 0f)
                {
                    compostableCache[def.itemid] = compostable.TotalFertilizerProduced;
                }
            }
        }

        private object OnOvenCook(Composter c, Item i)
        {
            if (c == null || c.IsDestroyed || c.inventory == null) return null;

            List<Item> items = c.inventory.itemList;
            int count = items.Count;
            if (count == 0) return null;

            int minSlot = c._inputSlotIndex;
            int maxSlot = minSlot + c.inputSlots;

            int cookingCount = 0;
            for (int idx = 0; idx < count; idx++)
            {
                Item item = items[idx];
                if (item != null && item.position >= minSlot && item.position < maxSlot)
                {
                    cookingCount++;
                }
            }

            if (cookingCount == 0) return null;

            float delta = 0.5f * (float)c.smeltSpeed / (float)cookingCount;
            ItemDefinition fertilizerDef = c.FertilizerDef != null ? c.FertilizerDef : defaultFertilizerDef;
            if (fertilizerDef == null) return null;

            List<Item> readyItems = null;

            for (int idx = 0; idx < count; idx++)
            {
                Item item = items[idx];
                if (item == null || item.info == null) continue;
                if (item.position < minSlot || item.position >= maxSlot) continue;
                if (!compostableCache.ContainsKey(item.info.itemid)) continue;

                if (item.cookTimeLeft - delta <= 0f)
                {
                    if (readyItems == null)
                    {
                        readyItems = Facepunch.Pool.Get<List<Item>>();
                    }
                    readyItems.Add(item);
                }
            }

            if (readyItems == null) return null;

            if (!permission.UserHasPermission(c.OwnerID.ToString(), permissionName))
            {
                Facepunch.Pool.FreeUnmanaged(ref readyItems);
                return null;
            }

            for (int idx = 0; idx < readyItems.Count; idx++)
            {
                Item item = readyItems[idx];
                float fertilizerProducedPerItem;
                if (!compostableCache.TryGetValue(item.info.itemid, out fertilizerProducedPerItem)) continue;

                int amount = item.amount;
                if (amount <= 0) continue;

                float totalFertilizerProduced = amount * fertilizerProducedPerItem;
                item.Remove();

                int fertilizerCount = Mathf.FloorToInt(totalFertilizerProduced);
                float remainder = totalFertilizerProduced - fertilizerCount;
                if (UnityEngine.Random.value < remainder)
                {
                    fertilizerCount++;
                }

                while (fertilizerCount > 0)
                {
                    int stackToCreate = Mathf.Min(fertilizerCount, fertilizerDef.stackable);
                    Item fertilizerItem = ItemManager.Create(fertilizerDef, stackToCreate, 0UL);
                    if (fertilizerItem == null) break;

                    if (!fertilizerItem.MoveToContainer(c.inventory))
                    {
                        fertilizerItem.Drop(c.inventory.dropPosition, c.inventory.dropVelocity);
                        c.OvenFull();
                    }

                    fertilizerCount -= stackToCreate;
                }
            }

            Facepunch.Pool.FreeUnmanaged(ref readyItems);
            return null;
        }

        #region Config
        static Configuration config;
        public class Configuration
        {
            [JsonProperty(PropertyName = "Debug")]
            public bool debug;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    debug = false
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
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion
    }
}
