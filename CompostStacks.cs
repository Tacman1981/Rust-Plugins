using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.0.3")]
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
                        composter.CompostEntireStack = CompostEntireStack; // Set to true by default
                    }
                    else
                    {
                        composter.CompostEntireStack = false; // Disable for unauthorized owners
                    }
                }
            }
        }

        private bool HasPermission(IPlayer player)
        {
            // Check if the player has the required permission
            return player.HasPermission(permissionName);
        }

        private void OnUserPermissionGranted(string id, string permission)
        {
            if (permission == permissionName)
            {
                timer.Once(1.0f, () => ReloadPlugin()); // Introduce a 1-second delay before reloading
            }
        }
        
        private void OnUserPermissionRevoked(string id, string permission)
        {
            if (permission == permissionName)
            {
                timer.Once(1.0f, () => ReloadPlugin()); // Introduce a 1-second delay before reloading
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
