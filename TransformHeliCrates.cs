using Oxide.Core;
using Oxide.Core.Plugins;
using Facepunch;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("TransformHeliCrates", "Tacman", "0.0.10")]
    [Description("Moves heli crates to the player who destroyed the helicopter.")]
    public class TransformHeliCrates : RustPlugin
    {
        private Dictionary<PatrolHelicopter, BasePlayer> heliKillers = new Dictionary<PatrolHelicopter, BasePlayer>();

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is PatrolHelicopter helicopter && info.InitiatorPlayer != null)
            {
                heliKillers[helicopter] = info.InitiatorPlayer;
                //Puts($"Tracking player {info.InitiatorPlayer.displayName} for helicopter damage.");
            }
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity is PatrolHelicopter helicopter)
            {
                if (heliKillers.TryGetValue(helicopter, out BasePlayer killer))
                {
                    //Puts($"Helicopter killed by {killer.displayName}");
                    heliKillers.Remove(helicopter);
                }
                else
                {
                    //Puts("No killer found for helicopter.");
                }
            }
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null || (entity.ShortPrefabName != "heli_crate" && entity.ShortPrefabName != "codelockedhackablecrate"))
            {
                return;
            }

            timer.Once(0.1f, () => CheckOwnerAndMove(entity));
        }

        private void CheckOwnerAndMove(BaseEntity entity)
        {
            ulong ownerId = entity.OwnerID;
            BasePlayer owner = BasePlayer.FindByID(ownerId);

            //Puts($"Crate spawned with OwnerID: {ownerId}");

            if (owner == null && ownerId == 0)
            {
                Puts("Owner not found for crate spawn, retrying...");
                timer.Once(0.1f, () => CheckOwnerAndMove(entity));
                return;
            }

            if (owner == null)
            {
                //Puts("Failed to identify crate owner after multiple attempts.");
                return;
            }

            Vector3 playerPosition = owner.transform.position;
            entity.transform.position = playerPosition + new Vector3(0, 1.5f, 0);
            entity.SendNetworkUpdateImmediate();

            Puts($"Moved {entity.ShortPrefabName} to {owner.displayName}'s position: {entity.transform.position}");

            timer.Once(1f, () =>
            {
                Rigidbody rb = entity.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = entity.gameObject.AddComponent<Rigidbody>();
                    //Puts($"Added Rigidbody to {entity.ShortPrefabName}");
                }
                rb.useGravity = true;
            });
        }

        void Unload()
        {
            heliKillers.Clear();
        }
    }
}
