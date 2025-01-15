//████████╗ █████╗  ██████╗███╗   ███╗ █████╗ ███╗   ██╗
//╚══██╔══╝██╔══██╗██╔════╝████╗ ████║██╔══██╗████╗  ██║
//   ██║   ███████║██║     ██╔████╔██║███████║██╔██╗ ██║
//   ██║   ██╔══██║██║     ██║╚██╔╝██║██╔══██║██║╚██╗██║
//   ██║   ██║  ██║╚██████╗██║ ╚═╝ ██║██║  ██║██║ ╚████║
//   ╚═╝   ╚═╝  ╚═╝ ╚═════╝╚═╝     ╚═╝╚═╝  ╚═╝╚═╝  ╚═══╝
using UnityEngine;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using System.Collections.Generic;
using Oxide.Core.Libraries;
using System.Numerics;

namespace Oxide.Plugins
{
    [Info("Buoyant Crates", "Tacman", "2.1.3")]
    [Description("Configurable crate buoyancy, add your own crates to config by shortname")]
    class BuoyantCrates : RustPlugin
    {
        #region Config

        public PluginConfig _config;

        public class PluginConfig
        {
            [JsonProperty("How long after the crate lands in water does it apply buoyancy (lower is faster)")]
            public int DetectionRate = 1;

            [JsonProperty("Buoyancy Scale (recommended to use between 0.7 and 2.0, any higher and it will be a trampoline)")]
            public float BuoyancyScale = 1f;

            [JsonProperty("Debug Mode")]
            public bool debugMode = false;

            [JsonProperty("Crates which will float (list by crate name)")]
            public List<string> CrateList = new List<string>();
        }

        protected override void LoadDefaultConfig()
        {
            _config = new PluginConfig
            {
                DetectionRate = 1,
                BuoyancyScale = 1f,
                debugMode = false,
                CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" }
            };
            Config.WriteObject(_config, true);
        }

        void Init()
        {
            Plugin shipwreckPlugin = plugins.Find("Shipwreck");
            Plugin convoyPlugin = plugins.Find("Convoy");
            Plugin trainEvent = plugins.Find("ArmoredTrain");

            if (trainEvent != null)
            {
                Puts("ArmoredTrain event plugin found, ignoring drag and gravity for train event.");
            }
            else
            {
                //Puts("ArmoredTrain plugin not found!");
            }

            if (shipwreckPlugin != null)
            {
                Puts("Shipwreck plugin found. Setting up buoyancy bypass for Shipwreck crates");
            }
            else
            {
                //Puts("Shipwreck plugin not found!");
            }

            if (convoyPlugin != null)
            {
                Puts("Convoy plugin found. Ignoring drag and gravity for Convoy crates.");
            }
            else
            {
                //Puts("Convoy plugin not found!");
            }

            _config = Config.ReadObject<PluginConfig>();

            if (_config.CrateList == null || _config.CrateList.Count == 0)
                _config.CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" };

            if (_config.DetectionRate == 0)
                _config.DetectionRate = 1;

            if (_config.BuoyancyScale == 0)
                _config.BuoyancyScale = 1f;

            Config.WriteObject(_config, true);
        }

        #endregion

        #region Spawn

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null || !(entity is StorageContainer crate) || !_config.CrateList.Contains(entity.ShortPrefabName))
            {
                return;
            }

            Plugin shipwreckPlugin = plugins.Find("Shipwreck");
            Plugin convoyPlugin = plugins.Find("Convoy");
            Plugin armoredTrainPlugin = plugins.Find("ArmoredTrain");

            // Delay the buoyancy application until the next tick
            NextTick(() =>
            {
                try
                {
                    if (Interface.CallHook("OnBuoyancyAdded", crate.net.ID.Value) != null)
                    {
                        if (_config.debugMode)
                        {
                            Puts($"Buoyancy addition interrupted for {crate.ShortPrefabName} by a plugin.");
                        }
                        return;
                    }
                    // Check if the crate still doesn't have a parent
                    if (crate.transform.parent != null)
                    {
                        var parentEntity = crate.transform.parent.gameObject;
                        if (_config.debugMode)
                        {
                            Puts($"Crate {crate.ShortPrefabName} is parented to {parentEntity.name}, skipping buoyancy.");
                        }
                        return;
                    }

                    // Check for specific plugin-related crates
                    if (armoredTrainPlugin != null && (bool)armoredTrainPlugin.Call("IsTrainCrate", crate.net.ID.Value))
                    {
                        if (_config.debugMode)
                        {
                            Puts($"ArmoredTrain crate detected: {crate.ShortPrefabName}");
                        }
                        return;
                    }

                    if (convoyPlugin != null && (bool)convoyPlugin.Call("IsConvoyCrate", crate))
                    {
                        if (_config.debugMode)
                        {
                            Puts($"Convoy crate detected: {crate.ShortPrefabName}");
                        }
                        return;
                    }

                    if (shipwreckPlugin != null && (bool)shipwreckPlugin.Call("IsShipwreckCrate", crate))
                    {
                        if (_config.debugMode)
                        {
                            Puts($"Shipwreck crate detected: {crate.ShortPrefabName}");
                        }
                        return;
                    }

                    // Add Rigidbody and Buoyancy if the crate is not parented
                    Rigidbody rb = crate.GetComponent<Rigidbody>() ?? crate.gameObject.AddComponent<Rigidbody>();
                    if (rb == null)
                    {
                        Puts($"Failed to add Rigidbody to crate: {crate.ShortPrefabName}");
                        return;
                    }

                    if (_config.CrateList.Contains("heli_crate") && crate.ShortPrefabName.Contains("heli_crate"))
                    {
                        rb.useGravity = true;
                        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                        rb.mass = 2f;
                        rb.interpolation = RigidbodyInterpolation.Interpolate;
                        rb.angularVelocity = Vector3Ex.Range(-2.75f, 2.75f);
                        rb.drag = 0.5f * (rb.mass / 2);

                        if (_config.debugMode)
                        {
                            Puts("Buoyancy applied to helicopter crate!");
                        }
                    }

                    if ((_config.CrateList.Contains("supply_drop") && crate.ShortPrefabName.Contains("supply_drop")) || (_config.CrateList.Contains("codelockedhackablecrate") && crate.ShortPrefabName.Contains("codelockedhackablecrate")))
                    {
                        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
                    }

                    // Add buoyancy component
                    MakeBuoyant buoyancy = crate.gameObject.AddComponent<MakeBuoyant>();
                    buoyancy.buoyancyScale = _config.BuoyancyScale;
                    buoyancy.detectionRate = _config.DetectionRate;

                    if (_config.debugMode)
                    {
                        Puts($"Buoyancy applied to crate: {crate.ShortPrefabName}");
                    }
                }
                catch (Exception ex)
                {
                    Puts($"{entity.ShortPrefabName} was not processed properly, nothing to worry about as the plugin will continue working normally : {ex.Message}");
                }
            });
        }

        #endregion

        #region Classes

        class MakeBuoyant : MonoBehaviour
        {
            public float buoyancyScale;
            public int detectionRate;
            private BaseEntity _entity;
            private SupplyDrop _supplyDrop;

            void Awake()
            {
                _entity = GetComponent<BaseEntity>();
                if (_entity == null)
                {
                    Destroy(this);
                    return;
                }

                _supplyDrop = _entity as SupplyDrop;
            }

            void FixedUpdate()
            {
                if (_entity == null)
                {
                    Destroy(this);
                    return;
                }

                Bounds bounds = new Bounds(_entity.WorldSpaceBounds().ToBounds().center, _entity.WorldSpaceBounds().ToBounds().size);

                if (UnityEngine.Time.frameCount % detectionRate == 0 && WaterLevel.Factor(bounds, true, true, _entity) > 0.65f)
                {
                    if (_supplyDrop != null)
                    {
                        _supplyDrop.RemoveParachute();
                        _supplyDrop.isLootable = true;
                    }
                    BuoyancyComponent(_entity);
                    Destroy(this);
                }
            }

            void BuoyancyComponent(BaseEntity entity)
            {
                Buoyancy buoyancy = entity.gameObject.AddComponent<Buoyancy>();
                buoyancy.buoyancyScale = buoyancyScale;
                buoyancy.rigidBody = entity.gameObject.GetComponent<Rigidbody>();
                buoyancy.rigidBody.velocity = UnityEngine.Vector3.zero;
                buoyancy.rigidBody.angularVelocity = UnityEngine.Vector3.zero;
                buoyancy.rigidBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX;
                buoyancy.SavePointData(true);
            }
        }

        #endregion
    }
}
