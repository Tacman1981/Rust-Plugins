using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System.Collections.Generic;
using Facepunch;
using ConVar;
using System.Linq;

/*
V 1.4.0: Changed OnPlayerDisconnected to use linq instead of unity FindObjectsOfType<>
V 1.3.0: Remove player from dictionary when they disconnect, hopefully fixing the process hanging.
V 1.2.0: Removed else from outputpiles returning early if conditions dont match, this should help with offline check and return default behaviour instead of processing custom code.
V 1.1.0: Added player offline check to revert to default if the player logs out while excavator is running. This should remove the long hook time hopefully. It will start feeding inventory when they reconnect again.
*/
namespace Oxide.Plugins
{
    [Info("Excavator Output", "Tacman", "1.4.0")]
    [Description("Plugin to manage resource distribution from Excavator output to player inventory.")]
    public class ExcavatorOutput : RustPlugin
    {
        // Dictionary to map excavator instances to player IDs
        private Dictionary<ExcavatorArm, ulong> excavatorPlayerMap = new Dictionary<ExcavatorArm, ulong>();

        private void Init()
        {
            Subscribe(nameof(OnExcavatorResourceSet));
            Subscribe(nameof(OnExcavatorGather));
        }

        private void OnExcavatorResourceSet(BaseEntity excavator, string resourceType, BasePlayer player)
        {
            if (excavator is ExcavatorArm arm)
            {
                // Store the excavator for the player
                excavatorPlayerMap[arm] = player.userID;
                Puts($"Excavator started by {player.displayName}. [This is here so we can track if the player disconnecting causes hanging]"); //Added this to log if player disconnecting is the cause of server hang.
                Chat.Broadcast($"{player.displayName} has set the excavator to {resourceType}.");
            }
        }

        private void OutputPiles(ExcavatorArm arm, Item item, BasePlayer player)
        {
            if (player == null || !player.IsConnected || player.inventory == null)
            {
                // Drop item if player is not found or offline
                item.Drop(arm.transform.position, Vector3.zero);
                return;
            }

            bool itemMoved = false;
            int remainingAmount = item.amount;

            // Attempt to add to existing stacks
            foreach (Item slot in player.inventory.containerMain.itemList)
            {
                if (slot.info.itemid == item.info.itemid && slot.amount < slot.MaxStackable())
                {
                    int spaceAvailable = slot.MaxStackable() - slot.amount;
                    int amountToAdd = Mathf.Min(remainingAmount, spaceAvailable);

                    slot.amount += amountToAdd;
                    slot.MarkDirty();
                    remainingAmount -= amountToAdd;

                    if (remainingAmount <= 0)
                    {
                        item.Remove();
                        itemMoved = true;
                        break;
                    }
                }
            }

            // Attempt to create new stacks for the remaining items
            while (!itemMoved && remainingAmount > 0)
            {
                Item newItem = ItemManager.CreateByItemID(item.info.itemid, remainingAmount);
                bool moved = newItem.MoveToContainer(player.inventory.containerMain);

                if (moved)
                {
                    remainingAmount -= newItem.amount;
                }
                else
                {
                    if (player.inventory.containerMain.IsFull())
                    {
                        // Drop item at the player's position if the inventory is full
                        newItem.Drop(player.transform.position, Vector3.zero);
                    }
                    else
                    {
                        Puts($"Failed to move new item {newItem.info.displayName.english} to {player.displayName}'s inventory. This might indicate an issue with the item or container.");
                    }

                    remainingAmount -= newItem.amount;
                    break;
                }
            }

            // Remove the original item if it has been fully processed
            if (remainingAmount <= 0)
            {
                item.Remove();
            }
        }

        private void OnExcavatorGather(BaseEntity excavator, Item item)
        {
            if (excavator is ExcavatorArm arm)
            {
                // Check if we have a player associated with this excavator
                if (excavatorPlayerMap.TryGetValue(arm, out ulong playerId))
                {
                    BasePlayer player = BasePlayer.FindByID(playerId);

                    OutputPiles(arm, item, player);
                }
                else
                {
                    return;
                }
            }
        }

        // Informing player about the plugin
        void OnPlayerConnected(BasePlayer player)
        {
            player.ChatMessage("Excavator Output: Output for excavator is your own inventory");
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // Create a list of ExcavatorArm instances to remove
            var armsToRemove = excavatorPlayerMap
                .Where(kvp => kvp.Value == player.userID)
                .Select(kvp => kvp.Key)
                .ToList();
        
            // Remove each ExcavatorArm from the dictionary and the scene
            foreach (var arm in armsToRemove)
            {
                excavatorPlayerMap.Remove(arm);
            }
        }

        private void Unload()
        {
            excavatorPlayerMap.Clear();
        }
    }
}
