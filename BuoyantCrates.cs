//Ultimate update, requires no further features. Will continue to be compiled for as long as I am able.

using UnityEngine;
using Rust;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Buoyant Crates", "Tacman", "2.0.0")]
    [Description("Makes helicopter and code locked hackable crates buoyant")]
    class BuoyantCrates : RustPlugin
    {
        #region Config
        
        public PluginConfig _config;

        public class PluginConfig
        {
            public int DetectionRate = 1;
            [JsonProperty("Delay after Shipwreck event starts (in seconds) until the floating crate functionality returns")]
            public float ShipwreckStartDelay = 5f;
            [JsonProperty("Buoyancy Scale (set this too high and it can have undesirable results)")]
            public float BuoyancyScale = 1f;
            [JsonProperty("debug")]
            public bool debugMode = false;
            [JsonProperty("Crates which will float(Uses Equals so shortname must be exact)")]
            public List<string> CrateList = new List<string>();
        }
        #endregion

        #region Oxide

        protected override void LoadDefaultConfig()
        {
            // You can provide a more detailed configuration setup if needed
            Config.WriteObject(new PluginConfig(), true);
        }

        void Init()
        {
            // Read the config
            _config = Config.ReadObject<PluginConfig>();

            // Ensure that the config has the required fields
            if (_config.CrateList == null || _config.CrateList.Count == 0)
            {
                // Initialize the CrateList with default values if not present
                _config.CrateList = new List<string> { "heli_crate", "codelockedhackablecrate", "supply_drop" };
                Config.WriteObject(_config, true); // Update the config file
            }
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
            if (_isShipwreckEventActive || entity == null || !_config.CrateList.Equals(entity.ShortPrefabName)) // Using Equals instead of Contains to ensure only the correct crates float.
            {
                return;
            }

            // Adjust position for heli_crate
            if (entity.ShortPrefabName == "heli_crate")
            {
                Vector3 originalPosition = entity.transform.position;
                entity.transform.position += new Vector3(0, 10f, 0);
                if (_config.debugMode)
                {
                    Puts($"Transformed position of {entity.ShortPrefabName} by {entity.transform.position - originalPosition}");
                }
            }

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
