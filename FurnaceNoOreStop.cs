using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Furnace No Ore Stop", "Tacman", "1.0.0")]
    [Description("Stops furnaces when there are no more ores, now stops when owner goes offline if permission 'onoff' granted")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private bool debug = false;
        private string usePerm = "furnacenoorestop.use";
        private string onOffPerm = "furnacenoorestop.onoff";

        #region Setup

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
            permission.RegisterPermission(onOffPerm, this);
        }

        #endregion

        #region Hooks

        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null) return;

            if (oven.ShortPrefabName.Contains("lantern") || oven.ShortPrefabName.Contains("bbq") || oven.ShortPrefabName.Contains("fire")) return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

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
                if (debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned off as it has no more ores/crude.");
            }
        }

        /*in here, it does not actually automatically turn on when the furnace already has existing items, so I use OnLootEntityEnd() to accomplish this.
         I have enabled this again so that industrial automation works properly.*/
        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            if (container == null || item == null)
                return;

            BaseOven oven = container.entityOwner as BaseOven;
            if (oven == null) return;

            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return;

            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if ((item.info.shortname.Contains("crude") || item.info.shortname.Contains("ore")) && !oven.IsOn())
            {
                oven.StartCooking();
                if (debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as ore/crude was added.");
            }
        }

        private void OnLootEntityEnd(BasePlayer player, BaseOven oven)
        {
            if (oven == null) return;
            if (!oven.ShortPrefabName.Contains("furnace") && !oven.ShortPrefabName.Contains("refinery")) return;
            if (!permission.UserHasPermission(oven.OwnerID.ToString(), usePerm)) return;

            if (oven.inventory.itemList.Any(i => i.info.shortname.Contains("ore") || i.info.shortname.Contains("crude")) && !oven.IsOn())
            {
                oven.StartCooking();
                if (debug)
                    Puts($"Furnace owned by {BasePlayer.FindByID(oven.OwnerID)?.displayName} has been turned on as it has ores/crude inside.");
            }
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            ToggleOvens(player, true);
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            ToggleOvens(player, false);
        }
        #endregion

        #region Helper

        private void ToggleOvens(BasePlayer player, bool online)
        {
            if (player == null || !permission.UserHasPermission(player.UserIDString, onOffPerm)) return;

            foreach (BaseOven oven in BaseNetworkable.serverEntities.OfType<BaseOven>())
            {
                if (oven.OwnerID != player.userID) continue;

                if (online && !oven.IsOn())
                {
                    oven.StartCooking();
                    if (debug)
                        Puts($"Furnace owned by {player.displayName} has been turned on as they connected.");
                }
                else if (!online && oven.IsOn())
                {
                    oven.StopCooking();
                    if (debug)
                        Puts($"Furnace owned by {player.displayName} has been turned off as they disconnected.");
                }
            }
        }
        #endregion
    }
}
