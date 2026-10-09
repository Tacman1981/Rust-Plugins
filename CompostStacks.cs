using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using Newtonsoft.Json;
using System;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.1.3")]
    [Description("Composts entire stacks of compostable items inside the composters. It no longer uses the boolean value")]
    public class CompostStacks : RustPlugin
    {
        private const string permissionName = "compoststacks.use";

        private void Init()
        {
            permission.RegisterPermission(permissionName, this);
        }

        private object OnOvenCook(Composter c, Item i)
        {
            if (c == null || c.IsDestroyed || c.inventory == null) return null;
            if (!permission.UserHasPermission(c.OwnerID.ToString(), permissionName)) return null;

            int cookingCount = 0;
            for (int index = 0; index < c.inventory.itemList.Count; index++)
            {
                Item item = c.inventory.itemList[index];
                if (item != null && item.position >= c._inputSlotIndex && item.position < c._inputSlotIndex + c.inputSlots)
                {
                    cookingCount++;
                }
            }

            if (cookingCount == 0) return null;

            float delta = 0.5f * (float)c.smeltSpeed / (float)cookingCount;
            ItemDefinition fertilizerDef = c.FertilizerDef != null ? c.FertilizerDef : ItemManager.FindItemDefinition("fertilizer");
            if (fertilizerDef == null) return null;

            List<Item> readyItems = Facepunch.Pool.Get<List<Item>>();
            for (int index = 0; index < c.inventory.itemList.Count; index++)
            {
                Item item = c.inventory.itemList[index];
                if (item == null || item.info == null) continue;
                if (item.position < c._inputSlotIndex || item.position >= c._inputSlotIndex + c.inputSlots) continue;

                ItemModCompostable compostable = item.info.GetComponent<ItemModCompostable>();
                if (compostable == null || compostable.TotalFertilizerProduced <= 0f) continue;

                if (item.cookTimeLeft - delta <= 0f)
                {
                    readyItems.Add(item);
                }
            }

            if (readyItems.Count == 0)
            {
                Facepunch.Pool.FreeUnmanaged(ref readyItems);
                return null;
            }

            for (int index = 0; index < readyItems.Count; index++)
            {
                Item item = readyItems[index];
                ItemModCompostable compostable = item.info.GetComponent<ItemModCompostable>();
                if (compostable == null) continue;

                int amount = item.amount;
                if (amount <= 0) continue;

                float totalFertilizerProduced = amount * compostable.TotalFertilizerProduced;
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
