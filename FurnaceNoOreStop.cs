using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("FurnaceNoOreStop", "Tacman", "1.0.1")]
    [Description("Stops furnaces when there are no more ores, retaining the fuel, and ensures lanterns are unaffected.")]
    public class FurnaceNoOreStop : RustPlugin
    {
        private void OnOvenCooked(BaseOven oven)
        {
            if (oven == null || oven.inventory == null)
                return;

            if (oven.ShortPrefabName.Contains("lantern"))
                return;

            bool hasOres = false;

            foreach (Item item in oven.inventory.itemList)
            {
                if (item.info.shortname.Contains("ore") || (item.info.shortname.Contains("crude")))
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
                    player.ChatMessage("Your cooking has stopped because there are no more cookables.");
                }
            }
        }
    }
}
