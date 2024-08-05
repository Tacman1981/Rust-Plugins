using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Oxide.Plugins;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "2.0.3")]
    [Description("Toggle the CompostEntireStack boolean on load and for all Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;
        private const string permissionName = "compoststacks.use"; // Permission name

        private Dictionary<string, Dictionary<string, string>> allMessages = new Dictionary<string, Dictionary<string, string>>();

        private void OnServerInitialized()
        {
            LoadDefaultMessages();
            RegisterMessages();
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
        }

        private void LoadDefaultMessages()
        {
            // English messages (default)
            Dictionary<string, string> englishMessages = new Dictionary<string, string>();
            englishMessages["NoPermission"] = "<color=red>This composter will not compost whole stacks.</color>";
            englishMessages["ComposterEnabled"] = "<color=green>This composter will compost whole stacks.</color>";

            // French messages
            Dictionary<string, string> frenchMessages = new Dictionary<string, string>();
            frenchMessages["NoPermission"] = "<color=red>Ce composteur ne compostera pas des piles entières.</color>";
            frenchMessages["ComposterEnabled"] = "<color=green>Ce composteur compostera des piles entières.</color>";

            // Add other language messages...

            // Add the messages to the dictionary of all messages
            allMessages["en"] = englishMessages;
            allMessages["fr"] = frenchMessages;
            // Add other languages to the dictionary...
        }

        private void RegisterMessages()
        {
            LoadMessages();
            foreach (var languageMessages in allMessages)
            {
                lang.RegisterMessages(languageMessages.Value, this, languageMessages.Key);
            }

            lang.RegisterMessages(allMessages["en"], this); // Register English messages separately as a fallback
        }

        private void LoadMessages()
        {
            // Load messages for the active language
            LoadDefaultMessages();
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter composter)
            {
                if (composter.OwnerID == 0)
                {
                    Puts("Composter OwnerID is 0 or invalid.");
                    return;
                }

                IPlayer ownerPlayer = covalence.Players.FindPlayerById(composter.OwnerID.ToString());

                if (ownerPlayer == null)
                {
                    Puts($"Owner player not found for OwnerID: {composter.OwnerID}");
                    return;
                }

                if (HasPermission(ownerPlayer))
                {
                    composter.CompostEntireStack = CompostEntireStack; // Set to true by default
                    ownerPlayer.Message(lang.GetMessage("ComposterEnabled", this, ownerPlayer.Id));
                }
                else
                {
                    composter.CompostEntireStack = false;
                    ownerPlayer.Message(lang.GetMessage("NoPermission", this, ownerPlayer.Id));
                }
            }
        }

        private void UpdateComposters()
        {
            foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
            {
                if (composter.OwnerID == 0)
                {
                    Puts("Composter OwnerID is 0 or invalid.");
                    continue;
                }

                IPlayer ownerPlayer = covalence.Players.FindPlayerById(composter.OwnerID.ToString());

                if (ownerPlayer == null)
                {
                    Puts($"Owner player not found for OwnerID: {composter.OwnerID}");
                    continue;
                }

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
