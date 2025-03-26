using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.0.3")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private string usePerm = "furnacenoorestop.use";
        private string onOffPerm = "furnacenoorestop.onoff";

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            permission.RegisterPermission(onOffPerm, this);
        }

        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null)
                return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm))
                return;

            if (oven.ShortPrefabName.Contains("lantern") || oven.ShortPrefabName.Contains("bbq") || oven.ShortPrefabName.Contains("fire"))
                return;

            bool hasOres = false;

            foreach (Item item in oven.inventory.itemList)
            {
                if (item.info.shortname.Contains("ore") || item.info.shortname.Contains("crude"))
                {
                    hasOres = true;
                    break;
                }
            }

            if (!hasOres)
            {
                oven.StopCooking();

                BasePlayer player = BasePlayer.FindByID(oven.OwnerID);
                if (player != null)
                {
                    //player.ChatMessage("Your cooking has stopped because there are no more cookables.");
                }
            }
        }

        void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container?.entityOwner == null || item == null)
                return;

            BaseOven oven = container.entityOwner as BaseOven;
            if (oven == null || (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")))
                return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm))
                return;

            if (item.info.shortname.Contains("crude") || item.info.shortname.Contains("ore"))
            {
                if (!oven.IsOn())
                {
                    oven.StartCooking();

                    BasePlayer player = BasePlayer.FindByID(oven.OwnerID);
                    if (player != null)
                    {
                        //player.ChatMessage("Your cooking has resumed because cookable items were added.");
                    }
                }
            }
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, onOffPerm))
            {
                var entities = BaseNetworkable.serverEntities;
                foreach (var entity in entities)
                {
                    if (entity is BaseOven oven && oven.OwnerID == player.userID)
                    {
                        oven.StopCooking();
                    }
                }
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            if (player != null && permission.UserHasPermission(player.UserIDString, onOffPerm))
            {
                var entities = BaseNetworkable.serverEntities;
                foreach (var entity in entities)
                {
                    if (entity is BaseOven oven && oven.OwnerID == player.userID)
                    {
                        oven.StartCooking();
                    }
                }
            }
        }
    }
}
