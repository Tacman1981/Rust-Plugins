//Still causing process hangs onexcavatorgather at random

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

        private void OnExcavatorGather(BaseEntity excavator, Item item)
        {
            if (excavator is ExcavatorArm arm)
            {
                if (excavatorPlayerMap.TryGetValue(arm, out ulong playerId))
                {
                    BasePlayer player = BasePlayer.FindByID(playerId);
        
                    if (player == null || !player.IsConnected)
                    {
                        // Drop item if the player is not connected
                        item.Drop(arm.transform.position, Vector3.zero);
                        return;
                    }
        
                    // Cache player's inventory
                    ItemContainer playerInventory = player.inventory.containerMain;
        
                    if (playerInventory == null)
                    {
                        // Drop item if the player's inventory is unavailable
                        item.Drop(arm.transform.position, Vector3.zero);
                        return;
                    }
        
                    OutputPiles(arm, item, player);
                }
            }
        }
        
        private void OutputPiles(ExcavatorArm arm, Item item, BasePlayer player)
        {
            if (player == null || player.inventory == null)
            {
                item.Drop(arm.transform.position, Vector3.zero);
                return;
            }
        
            ItemContainer inventory = player.inventory.containerMain;
            bool itemMoved = false;
            int remainingAmount = item.amount;
        
            // Attempt to add to existing stacks
            foreach (Item slot in inventory.itemList)
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
                bool moved = newItem.MoveToContainer(inventory);
        
                if (moved)
                {
                    remainingAmount -= newItem.amount;
                }
                else
                {
                    // Drop item at the player's position if the inventory is full or failed to move
                    newItem.Drop(player.transform.position, Vector3.zero);
                    remainingAmount -= newItem.amount;
                    break;
                }
            }
        
            if (remainingAmount <= 0)
            {
                item.Remove();
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
