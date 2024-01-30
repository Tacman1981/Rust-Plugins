using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "1.8.0")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private string permissionName = "compoststacks.use"; // Customize the permission name

        private void OnServerInitialized()
        {
            UpdateComposters();
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter)
            {
                Composter composter = entity as Composter;

                // Check if the player has the required permission
                if (HasPermission(composter.OwnerID))
                {
                    composter.CompostEntireStack = CompostEntireStack;
                }
            }
        }

        private void UpdateComposters()
        {
            foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
            {
                // Check if the player has the required permission
                if (HasPermission(composter.OwnerID))
                {
                    composter.CompostEntireStack = CompostEntireStack;
                }
            }
        }

        private bool HasPermission(ulong playerId)
        {
            // Check if the player has the required permission
            return permission.UserHasPermission(playerId.ToString(), permissionName);
        }
    }
}
