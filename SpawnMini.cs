using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Plugins
{
    [Info("SpawnMini", "Tacman", "1.0.0")]
    [Description("A plugin to spawn a minicopter through a command")]

    public class SpawnMini : RustPlugin
    {
        // This method is executed when the plugin is loaded
        void Init()
        {
            // Register a new command with the chat system and require permission
            AddCovalenceCommand("mini", "SpawnMiniCommand", "spawnmini.usecommand");
        }

        // This method is called when the command is executed
        private void SpawnMiniCommand(IPlayer player, string command, string[] args)
        {
            // Check if the player has permission to use the command
            if (!player.HasPermission("spawnmini.usecommand"))
            {
                player.Reply("You don't have permission to use this command.");
                return;
            }

            // Check the player's scrap balance
            int scrapCost = 1500;
            int playerScrap = 0;

            var basePlayer = player.Object as BasePlayer;
            if (basePlayer != null)
            {
                var scrapItemDefinition = ItemManager.FindItemDefinition("scrap");
                playerScrap = basePlayer.inventory.GetAmount(scrapItemDefinition.itemid);
            }

            if (playerScrap < scrapCost)
            {
                player.Reply($"You don't have enough scrap. You need {scrapCost} scrap to spawn a minicopter.");
                return;
            }

            // Deduct the scrap cost
            if (basePlayer != null)
            {
                var scrapItemDefinition = ItemManager.FindItemDefinition("scrap");
                int remainingScrap = scrapCost;

                for (int i = basePlayer.inventory.containerMain.itemList.Count - 1; i >= 0; i--)
                {
                    var item = basePlayer.inventory.containerMain.itemList[i];
                    if (item.info.itemid == scrapItemDefinition.itemid)
                    {
                        if (remainingScrap >= item.amount)
                        {
                            remainingScrap -= item.amount;
                            item.Remove();
                        }
                        else
                        {
                            item.amount -= remainingScrap;
                            remainingScrap = 0;
                            break;
                        }
                    }
                }

                if (remainingScrap > 0)
                {
                    player.Reply($"An error occurred while deducting scrap. Please try again.");
                    return;
                }
            }
            else
            {
                player.Reply("An error occurred while deducting scrap. Please try again.");
                return;
            }

            // Perform a raycast to get the spawn position and rotation
            RaycastHit hit;
            if (Physics.Raycast(basePlayer.eyes.HeadRay(), out hit))
            {
                Vector3 position = hit.point;
                Quaternion rotation = Quaternion.LookRotation(hit.normal, Vector3.up);

                // Debug lines to check raycast results
                Debug.Log($"Raycast hit position: {position}");
                Debug.Log($"Raycast hit normal: {hit.normal}");
                Debug.Log($"Rotation Euler angles: {rotation.eulerAngles}");

                // Calculate the adjusted rotation to align the minicopter correctly
                Vector3 adjustedEulerAngles = new Vector3(0, rotation.eulerAngles.y, 0);
                Quaternion adjustedRotation = Quaternion.Euler(adjustedEulerAngles);

                // Spawn the minicopter by creating an entity with position and rotation
                var entity = GameManager.server.CreateEntity("assets/content/vehicles/minicopter/minicopter.entity.prefab", position, adjustedRotation);
                if (entity != null)
                {
                    entity.Spawn();
                }
                else
                {
                    player.Reply("An error occurred while spawning the minicopter. Please try again.");
                    return;
                }

                // Optionally, you can send a message to the player to confirm the execution
                player.Reply("Minicopter spawned!");
            }
            else
            {
                player.Reply("Unable to find a suitable spawn location for the minicopter.");
            }
        }
    }
}
