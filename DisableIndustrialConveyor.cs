using Oxide.Core;
using Rust;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("DisableIndustrialConveyor", "Tacman", "1.5.1")]
    [Description("Disables placement of Industrial Conveyors on the server.")]
    public class DisableIndustrialConveyor : RustPlugin
    {
        private void OnServerInitialized()
        {
            permission.RegisterPermission("disableindustrialconveyor.bypass", this);

            // Define the translations for each language
            Dictionary<string, Dictionary<string, string>> messages = new Dictionary<string, Dictionary<string, string>>
            {
                ["en"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Industrial Conveyors are disabled on this server."
                },
                ["fr"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Les transporteurs industriels sont désactivés sur ce serveur."
                },
                ["de"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Industrieförderer sind auf diesem Server deaktiviert."
                },
                ["es"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Los transportadores industriales están desactivados en este servidor."
                },
                ["ru"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Промышленные конвейеры отключены на этом сервере."
                },
                ["zh"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "本服务器禁止使用工业传送带。"
                },
                ["nl"] = new Dictionary<string, string>
                {
                    ["NotAllowed"] = "Industriële transportbanden zijn uitgeschakeld op deze server."
                }
            };

            // Loop through each language and create the language file if it doesn't exist
            foreach (string code in messages.Keys)
            {
                string fileName = $"{code}.json";
                string filePath = $"{Interface.Oxide.LangDirectory}/{Name}/{fileName}";

                // Create the language file if it doesn't exist
                if (!Interface.Oxide.DataFileSystem.ExistsDatafile(filePath))
                {
                    lang.RegisterMessages(messages[code], this, code);
                    Puts($"Created {code} language file.");
                }
            }
        }

        private object CanBuild(Planner planner, Construction prefab)
        {
            if (prefab.fullName.Contains("industrialconveyor"))
            {
                var player = planner.GetOwnerPlayer();
                if (player != null && !permission.UserHasPermission(player.UserIDString, "disableindustrialconveyor.bypass"))
                {
                    player.ChatMessage(lang.GetMessage("NotAllowed", this, player.UserIDString));
                    return false;
                }
            }

            return null;
        }
    }
}
