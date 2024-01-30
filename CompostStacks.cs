using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "1.8.0")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private const string permissionName = "compoststacks.use"; // Permission name

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter composter)
            {
                IPlayer ownerPlayer = covalence.Players.FindPlayerById(composter.OwnerID.ToString());

                if (ownerPlayer != null)
                {
                    composter.CompostEntireStack = true; // Set to true by default

                    if (!HasPermission(ownerPlayer))
                    {
                        return;
                    }
                    else
                    {
                        ownerPlayer.Message("<color=green>This composter can compost entire stacks!</color>");
                    }
                }
            }
        }

        private void UpdateComposters()
        {
            foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
            {
                IPlayer ownerPlayer = covalence.Players.FindPlayerById(composter.OwnerID.ToString());

                if (ownerPlayer != null)
                {
                    composter.CompostEntireStack = true; // Set to true by default

                    if (!HasPermission(ownerPlayer))
                    {
                        composter.CompostEntireStack = false; // Disable for unauthorized owners
                        // Optionally add logging here to track disabled composters
                    }
                }
            }
        }

        private bool HasPermission(IPlayer player)
        {
            // Check if the player has the required permission
            return player.HasPermission(permissionName);
        }
    }
}
