using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.0.4")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private const string permissionName = "compoststacks.use"; // Permission name

        private Dictionary<string, Dictionary<string, string>> allMessages = new Dictionary<string, Dictionary<string, string>>();

        private void OnServerInitialized()
        {
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
        }
        
       private void OnPermissionRegistered(string name, CompostStacks owner)
        {
            if (permission != null && owner != null)
            {
                //Log permission registry to console, comment out this next line if you dont want this.
                Puts($"{permissionName} has been registered {(owner != null ? $"for {owner.Title}" : "")}");
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter composter)
            {
                if (composter == null) return;
        
                IPlayer ownerPlayer = covalence.Players.FindPlayerById(composter.OwnerID.ToString());
        
                if (ownerPlayer != null)
                {
                    if (HasPermission(ownerPlayer))
                    {
                        composter.CompostEntireStack = CompostEntireStack; // Set to true by default
                    }
                    else
                    {
                        composter.CompostEntireStack = false;
                    }
                }
                else
                {
                    Puts($"Owner player not found for OwnerID: {composter.OwnerID}");
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
                    if (HasPermission(ownerPlayer))
                    {
                        composter.CompostEntireStack = CompostEntireStack; //True if permission granted.
                    }
                    else
                    {
                        composter.CompostEntireStack = false; //set to false if no permission
                    }
                }
            }
        }

        private bool HasPermission(IPlayer player)
        {
            return player.HasPermission(permissionName);
        }

        private void OnUserPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to user {id}");
                timer.Once(1.0f, () => ReloadPlugin());
            }
        }
        
        private void OnUserPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to user {id}");
                timer.Once(1.0f, () => ReloadPlugin());
            }
        }

        private void OnGroupPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to group {id}");
                timer.Once(1.0f, () => ReloadPlugin());
            }
        }
        
        private void OnGroupPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                Puts($"Permission {permissionName} granted to group {id}");
                timer.Once(1.0f, () => ReloadPlugin());
            }
        }
        
        private void ReloadPlugin()
        {
            #if RUST
            SendConsoleCommand("o.reload CompostStacks");
            #endif
        }
        
        private void SendConsoleCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                return;
        
            covalence.Server.Command(command);
        }
    }
}
