using UnityEngine;
using Rust;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;

namespace Oxide.Plugins
{
    [Info("Buoyant Crates", "Tacman", "2.1.0")]
    [Description("Configurable crate buoyancy, add your own crates to config by shortname")]
    class BuoyantCrates : RustPlugin
    {

        [PluginReference] Plugin Shipwreck;

        #region Config

        public PluginConfig _config;

        public class PluginConfig
        {
            [JsonProperty("How long after the crate lands in water does it apply buoyancy (lower is faster)")]
            public int DetectionRate = 1;

            [JsonProperty("Transform position of helicopter crates upwards by this much (set to 0 for no transform)")]
            public int transformY = 10;

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
                transformY = 10,
                BuoyancyScale = 1f,
                debugMode = false,
                CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" }
            };
            Config.WriteObject(_config, true);
        }

        void Init()
        {
            Plugin shipwreckPlugin = plugins.Find("Shipwreck");

            if (shipwreckPlugin != null)
            {
                Puts("Shipwreck plugin found. Setting up buoyancy bypass for Shipwreck crates");
            }
            else
            {
                Puts("Shipwreck plugin not found!");
            }

            _config = Config.ReadObject<PluginConfig>();

            if (_config.CrateList == null || _config.CrateList.Count == 0)
                _config.CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" };

            if (_config.DetectionRate == null)
                _config.DetectionRate = 1;

            if (_config.transformY == null)
                _config.transformY = 10;

            if (_config.BuoyancyScale == 0)
                _config.BuoyancyScale = 0.8f;

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

            NextTick(() =>
            {
                if (shipwreckPlugin != null)
                {
                    if ((bool)shipwreckPlugin.Call("IsShipwreckCrate", crate))
                    {
                        return;
                    }
                }
                else
                {
                    if (_config.debugMode)
                    {
                        Puts("This is not a Shipwreck crate, applying buoyancy.");
                    }
                }

                if (_config.CrateList.Contains(crate.ShortPrefabName))
                {
                    if (entity.PrefabName.Contains("heli_crate"))
                    {
                        Vector3 originalPosition = crate.transform.position;
                        crate.transform.position += new Vector3(0, _config.transformY, 0);
                        if (_config.debugMode)
                        {
                            Puts($"Transformed position of {crate.ShortPrefabName} by {crate.transform.position - originalPosition}");
                        }
                    }

                    NextTick(() =>
                    {
                        Rigidbody rb = crate.GetComponent<Rigidbody>();
                        if (rb == null)
                        {
                            rb = crate.gameObject.AddComponent<Rigidbody>();
                        }

                        if (rb == null)
                        {
                            Puts("Rigidbody could not be added to crate.");
                            return;
                        }

                        rb.useGravity = true;
                        rb.collisionDetectionMode = (CollisionDetectionMode)2;
                        rb.mass = 2f;
                        rb.interpolation = (RigidbodyInterpolation)1;
                        rb.angularVelocity = Vector3Ex.Range(-1.75f, 1.75f);
                        rb.drag = 0.5f * rb.mass;
                        rb.angularDrag = 0.2f * (rb.mass / 5f);
                        MakeBuoyant buoyancy = crate.gameObject.AddComponent<MakeBuoyant>();
                        buoyancy.buoyancyScale = _config.BuoyancyScale;
                        buoyancy.detectionRate = _config.DetectionRate;
                    });
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
                buoyancy.rigidBody.velocity = Vector3.zero;
                buoyancy.rigidBody.angularVelocity = Vector3.zero;
                buoyancy.rigidBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX;
                buoyancy.SavePointData(true);
            }
        }

        #endregion
    }
}
