//Some changes to facepunch code has broken this 1. when it does work it has a delay when switching states.
//Still trying to figure out why it desnt continue to work after restarts. we likely must kill the minicopter on initialize to ensure continuous functionality.
//This is specifically designed for pve servers with personal minicopters.

//Special thanks to MinicopterOptions developer for this method, I literally just used the code from that plugin to attach the light, and the method used to toggle the light on and off. now we have night lights on minicopters, and can use jet engine from skill tree with no interference.
using System.Collections.Generic;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("MiniLight", "Tacman", "1.0.0")]
    [Description("Adds a search light to Minicopters.")]
    public class MiniLight : RustPlugin
    {
        #region Configuration
        private readonly string minicopterPrefab = "assets/content/vehicles/minicopter/minicopter.entity.prefab";
        private readonly string searchLightPrefab = "assets/prefabs/deployable/search light/searchlight.deployed.prefab";
        private readonly string spherePrefab = "assets/prefabs/visualization/sphere.prefab";

        private static readonly Vector3 searchLightOffset = new Vector3(0, 0.24f, 1.8f);
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            Subscribe(nameof(OnEntitySpawned));
            Subscribe(nameof(OnServerCommand));
        }

        private void OnServerInitialized()
        {
            // Get all Minicopters in the server
            foreach (BaseNetworkable entity in BaseNetworkable.serverEntities)
            {
                if (entity is Minicopter copter)
                {
                    OnEntitySpawned(copter);
                }
            }
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Minicopter copter)
            {
                timer.Once(1f, () => AddSearchLight(copter));
            }
        }

        private void Unload()
        {
            // Perform any necessary cleanup
        }
        #endregion

        #region Search Light Attachment
        private void SetupSphereEntity(SphereEntity sphereEntity)
        {
            sphereEntity.EnableSaving(true);
            sphereEntity.EnableGlobalBroadcast(false);
        }

        private void SetupSearchLight(SearchLight searchLight)
        {
            searchLight.pickup.enabled = false;
            DestroyMeshCollider(searchLight);
            DestroyGroundComp(searchLight);
        }

        private void AddSearchLight(Minicopter copter)
        {
            var sphereEntity = GameManager.server.CreateEntity(spherePrefab, new Vector3(0, -100, 0), Quaternion.identity) as SphereEntity;
            if (sphereEntity == null)
                return;

            SetupSphereEntity(sphereEntity);
            sphereEntity.SetParent(copter);
            sphereEntity.Spawn();

            var searchLight = GameManager.server.CreateEntity(searchLightPrefab, sphereEntity.transform.position) as SearchLight;
            if (searchLight == null)
                return;

            SetupSearchLight(searchLight);
            searchLight.Spawn();
            SetupInvincibility(searchLight);
            searchLight.SetFlag(BaseEntity.Flags.Reserved5, true);
            searchLight.SetFlag(BaseEntity.Flags.Busy, true);
            searchLight.SetParent(sphereEntity);
            searchLight.transform.localPosition = Vector3.zero;
            searchLight.transform.localRotation = Quaternion.Euler(-20, 180, 180);

            sphereEntity.currentRadius = 0.1f;
            sphereEntity.lerpRadius = 0.1f;
            sphereEntity.UpdateScale();
            sphereEntity.SendNetworkUpdateImmediate();

            timer.Once(3f, () =>
            {
                if (sphereEntity != null)
                {
                    sphereEntity.transform.localPosition = searchLightOffset;
                }
            });
        }

        private void SetupInvincibility(BaseCombatEntity entity)
        {
            entity._maxHealth = 99999999f;
            entity._health = 99999999f;
            entity.SendNetworkUpdate();
        }

        private void DestroyGroundComp(BaseEntity ent)
        {
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<DestroyOnGroundMissing>());
            UnityEngine.Object.DestroyImmediate(ent.GetComponent<GroundWatch>());
        }

        private void DestroyMeshCollider(BaseEntity ent)
        {
            foreach (var mesh in ent.GetComponentsInChildren<MeshCollider>())
            {
                UnityEngine.Object.DestroyImmediate(mesh);
            }
        }
        #endregion

        #region Command Handling
        private object OnServerCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.cmd.FullName != "inventory.lighttoggle")
                return null;

            var player = arg.Player();
            if (player == null)
                return null;

            var mini = player.GetMountedVehicle() as Minicopter;
            if (mini == null)
                return null;

            if (!mini.IsDriver(player))
                return null;

            foreach (var child in mini.children)
            {
                var sphere = child as SphereEntity;
                if (sphere == null)
                    continue;

                foreach (var grandChild in sphere.children)
                {
                    var light = grandChild as SearchLight;
                    if (light == null)
                        continue;

                    light.SetFlag(IOEntity.Flag_HasPower, !light.IsPowered());

                    // Prevent other lights from toggling.
                    return false;
                }
            }

            return null;
        }
        #endregion
    }
}

