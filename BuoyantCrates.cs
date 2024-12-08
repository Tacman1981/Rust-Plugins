using UnityEngine;
using Rust;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Buoyant Crates", "Tacman", "2.0.2")]
    [Description("Makes helicopter and code locked hackable crates buoyant")]
    class BuoyantCrates : RustPlugin
    {
        #region Config

        public PluginConfig _config;

        public class PluginConfig
        {
            [JsonProperty("How long after the crate lands in water does it apply buoyancy (lower is faster)")]
            public int DetectionRate = 1;
            [JsonProperty("Transform position of helicopter crates upwards by this much")]
            public int transformY = 10;
            [JsonProperty("Delay after Shipwreck event starts (in seconds) until the floating crate functionality returns")]
            public float ShipwreckStartDelay = 5f;
            [JsonProperty("Buoyancy Scale (set this too high and it can have undesirable results)")]
            public float BuoyancyScale = 1f;
            [JsonProperty("debug")]
            public bool debugMode = false;
            [JsonProperty("Crates which will float (Uses Equals so shortname must be exact)")]
            public List<string> CrateList = new List<string>();
        }
        #endregion

        #region Oxide

        protected override void LoadDefaultConfig()
        {
            // Set default config values directly
            _config = new PluginConfig
            {
                DetectionRate = 1,
                transformY = 10,
                ShipwreckStartDelay = 5f,
                BuoyancyScale = 1f,
                debugMode = false,
                CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" }
            };

            // Write the default config to the file
            Config.WriteObject(_config, true);
        }

        void Init()
        {
            // Load the config
            _config = Config.ReadObject<PluginConfig>();

            // Ensure that the config has the required fields
            if (_config.CrateList == null || _config.CrateList.Count == 0)
            {
                _config.CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" };
            }

            // Check and set default values for other fields if they are missing
            if (_config.DetectionRate == null)
                _config.DetectionRate = 1;

            if (_config.transformY == null)
                _config.transformY = 10;

            if (_config.ShipwreckStartDelay == null)
                _config.ShipwreckStartDelay = 5f;

            if (_config.BuoyancyScale == null)
                _config.BuoyancyScale = 1f;

            // Update the config file if any changes were made
            Config.WriteObject(_config, true);
        }

        #endregion

        #region ShipwreckEvent

        private bool _isShipwreckEventActive = false;

        void OnShipwreckStart()
        {
            _isShipwreckEventActive = true;
            timer.Once(_config.ShipwreckStartDelay, () =>
            {
                _isShipwreckEventActive = false;
            });
        }

        #endregion

        #region Spawn

        void OnEntitySpawned(BaseEntity entity)
        {
            if (_isShipwreckEventActive || entity == null || !_config.CrateList.Contains(entity.ShortPrefabName))
            {
                return;
            }

            // Adjust position for heli_crate
            if (entity.ShortPrefabName == "heli_crate")
            {
                Vector3 originalPosition = entity.transform.position;
                entity.transform.position += new Vector3(0, _config.transformY, 0);
                if (_config.debugMode)
                {
                    Puts($"Transformed position of {entity.ShortPrefabName} by {entity.transform.position - originalPosition}");
                }

                // Delay checking for the Rigidbody. This allows crates spawned using F1 to fall to the ground from the set transformY setting. previously this would leave crates floating in the air after using f1 to spawn heli_crate
                timer.Once(1f, () =>
                {
                    Rigidbody rb = entity.GetComponent<Rigidbody>();
                    if (rb == null)
                    {
                        rb = entity.gameObject.AddComponent<Rigidbody>();
                    }
                    rb.useGravity = true; // Ensure gravity is applied so that it really can fall down after using F! to "spawn heli_crate"
                });
            }

            // Existing buoyancy logic for other entities
            MakeBuoyant buoyancy = entity.gameObject.AddComponent<MakeBuoyant>();
            buoyancy.buoyancyScale = _config.BuoyancyScale;
            buoyancy.detectionRate = _config.DetectionRate;
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
