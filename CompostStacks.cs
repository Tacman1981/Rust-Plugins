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
            LoadDefaultMessages();
            RegisterMessages();
            permission.RegisterPermission(permissionName, this);
            UpdateComposters();
        }
        
        //I cannot guarantee accuracy of the following languages, you can adjust as required in language. I will no longer add a bunch of default languages but just english with support for others...
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

    // German messages
    Dictionary<string, string> germanMessages = new Dictionary<string, string>();
    germanMessages["NoPermission"] = "<color=red>Dieser Komposter wird keine ganzen Stapel kompostieren.</color>";
    germanMessages["ComposterEnabled"] = "<color=green>Dieser Komposter wird ganze Stapel kompostieren.</color>";

    // Spanish messages
    Dictionary<string, string> spanishMessages = new Dictionary<string, string>();
    spanishMessages["NoPermission"] = "<color=red>Este compostador no compostará pilas enteras.</color>";
    spanishMessages["ComposterEnabled"] = "<color=green>Este compostador compostará pilas enteras.</color>";

    // Italian messages
    Dictionary<string, string> italianMessages = new Dictionary<string, string>();
    italianMessages["NoPermission"] = "<color=red>Questo compostatore non composterà pile intere.</color>";
    italianMessages["ComposterEnabled"] = "<color=green>Questo compostatore composterà pile intere.</color>";

    // Russian messages
    Dictionary<string, string> russianMessages = new Dictionary<string, string>();
    russianMessages["NoPermission"] = "<color=red>Этот компостер не будет компостировать целые стопки.</color>";
    russianMessages["ComposterEnabled"] = "<color=green>Этот компостер будет компостировать целые стопки.</color>";

    // Chinese messages
    Dictionary<string, string> chineseMessages = new Dictionary<string, string>();
    chineseMessages["NoPermission"] = "<color=red>这个堆肥桶不会堆肥整个堆。</color>";
    chineseMessages["ComposterEnabled"] = "<color=green>这个堆肥桶将堆肥整个堆。</color>";

    // Ukrainian messages
    Dictionary<string, string> ukrainianMessages = new Dictionary<string, string>();
    ukrainianMessages["NoPermission"] = "<color=red>Цей компостер не буде компостувати цілі стопки.</color>";
    ukrainianMessages["ComposterEnabled"] = "<color=green>Цей компостер буде компостувати цілі стопки.</color>";

    // Portuguese messages
    Dictionary<string, string> portugueseMessages = new Dictionary<string, string>();
    portugueseMessages["NoPermission"] = "<color=red>Este compostor não compostará pilhas inteiras.</color>";
    portugueseMessages["ComposterEnabled"] = "<color=green>Este compostor compostará pilhas inteiras.</color>";

    // Finnish messages
    Dictionary<string, string> finnishMessages = new Dictionary<string, string>();
    finnishMessages["NoPermission"] = "<color=red>Tämä kompostori ei kompostoi koko pinoja.</color>";
    finnishMessages["ComposterEnabled"] = "<color=green>Tämä kompostori kompostoi koko pinoja.</color>";

    // Swedish messages
    Dictionary<string, string> swedishMessages = new Dictionary<string, string>();
    swedishMessages["NoPermission"] = "<color=red>Denna kompostbehållare kommer inte kompostera hela högar.</color>";
    swedishMessages["ComposterEnabled"] = "<color=green>Denna kompostbehållare kommer kompostera hela högar.</color>";

    // Norwegian messages
    Dictionary<string, string> norwegianMessages = new Dictionary<string, string>();
    norwegianMessages["NoPermission"] = "<color=red>Denne kompostbeholderen vil ikke kompostere hele stabelen.</color>";
    norwegianMessages["ComposterEnabled"] = "<color=green>Denne kompostbeholderen vil kompostere hele stabelen.</color>";

    // Japanese messages
    Dictionary<string, string> japaneseMessages = new Dictionary<string, string>();
    japaneseMessages["NoPermission"] = "<color=red>この堆肥器は整ったスタックを堆肥化しません。</color>";
    japaneseMessages["ComposterEnabled"] = "<color=green>この堆肥器は整ったスタックを堆肥化します。</color>";

    // Arabic messages
    Dictionary<string, string> arabicMessages = new Dictionary<string, string>();
    arabicMessages["NoPermission"] = "<color=red>هذا المُسمد لن يتسمم الكومة بأكملها.</color>";
    arabicMessages["ComposterEnabled"] = "<color=green>هذا المُسمد سيتسمم الكومة بأكملها.</color>";

    // Thai messages
    Dictionary<string, string> thaiMessages = new Dictionary<string, string>();
    thaiMessages["NoPermission"] = "<color=red>บริเวณการตกปลายนี้จะไม่ทำการหมักทั้งสต็อก.</color>";
    thaiMessages["ComposterEnabled"] = "<color=green>บริเวณการตกปลายนี้จะทำการหมักทั้งสต็อก.</color>";

    // Add the messages to the dictionary of all messages
    allMessages["en"] = englishMessages;
    allMessages["fr"] = frenchMessages;
    allMessages["de"] = germanMessages;
    allMessages["es"] = spanishMessages;
    allMessages["it"] = italianMessages;
    allMessages["ru"] = russianMessages;
    allMessages["zh"] = chineseMessages;
    allMessages["uk"] = ukrainianMessages;
    allMessages["pt"] = portugueseMessages;
    allMessages["fi"] = finnishMessages;
    allMessages["sv"] = swedishMessages;
    allMessages["no"] = norwegianMessages;
    allMessages["ja"] = japaneseMessages;
    allMessages["ar"] = arabicMessages;
    allMessages["th"] = thaiMessages;
    // Add messages for other languages as needed...
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
            // Replace with loading messages from a file or other source
            // For simplicity, using the default English messages
            LoadDefaultMessages();
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
                        ownerPlayer.Message(lang.GetMessage("ComposterEnabled", this, ownerPlayer.Id));
                    }
                    else
                    {
                        composter.CompostEntireStack = false;
                        ownerPlayer.Message(lang.GetMessage("NoPermission", this, ownerPlayer.Id));
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
