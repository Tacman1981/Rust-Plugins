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
        private const string permissionName = "compoststacks.use"; // Permission name

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
        }

        private void OnEntityBuilt(Planner plan, GameObject go)
        {
            // Check if the placed entity is a Composter
            if (go.GetComponent<Composter>() != null)
            {
                ulong playerID = plan.GetOwnerPlayer().userID;

                // Check if the player has the required permission
                if (HasPermission(playerID))
                {
                    Composter composter = go.GetComponent<Composter>();
                    composter.CompostEntireStack = CompostEntireStack;
                }
            }
        }

        private void UpdateComposters()
        {
            foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
            {
                // This will apply to all existing composters on server initialization.
                ulong ownerID = composter.OwnerID;

                // Check if the player has the required permission
                if (HasPermission(ownerID))
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
