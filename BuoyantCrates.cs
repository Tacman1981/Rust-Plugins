using UnityEngine;
using Rust;
using System;
using Oxide.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Buoyant Crates", "Tacman", "1.8.0")]
    [Description("Makes helicopter and code locked hackable crates buoyant")]
    class BuoyantCrates : RustPlugin
    {
        #region Config

        public PluginConfig _config;

        public PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                DetectionRate = 1,
                ShipwreckStartDelay = 5f, // Configurable delay to toggle off _isShipwreckEventActive.
                BuoyancyScale = 1f
            };
        }

        public class PluginConfig
        {
            public int DetectionRate = 1;
            [JsonProperty("This setting controls delay after shipwreck starts before it returns to normal(in seconds), if you have Shipwreck plugin.")]
            public float ShipwreckStartDelay = 5f; // Delay in seconds
            [JsonProperty("Buoyancy Scale (set this too high and it can have undesirable results)")]
            public float BuoyancyScale = 1f;
        }

        #endregion

        #region Oxide

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
            Config.WriteObject(_config, true);
        }

        #endregion

        #region ShipwreckEvent

        private bool _isShipwreckEventActive = false;

        void OnShipwreckStart()
        {
            _isShipwreckEventActive = true;

            //Wait your configured time here then turn off the event marker, so it doesnt interfere with normal plugin behaviour for the length of the event. Fingers crossed
            timer.Once(_config.ShipwreckStartDelay, () =>
            {
                _isShipwreckEventActive = false;
            });
        }

        #endregion

        #region Spawn

        void OnEntitySpawned(BaseEntity entity)
        {
            if (_isShipwreckEventActive)
            {
                return;
            }

            if (entity == null || (entity.ShortPrefabName != "heli_crate" && entity.ShortPrefabName != "codelockedhackablecrate" && entity.ShortPrefabName != "supply_drop")) return;

            //Added this transform to prevent crates being pushed underground when helis die on land, you can adjust the 5f to whatever is required. This also prevents gibs pulling crates under the water
            if (entity.ShortPrefabName == "heli_crate")
            {
                Vector3 currentPosition = entity.transform.position;

                Vector3 newPosition = currentPosition + new Vector3(0, 5f, 0);

                entity.transform.position = newPosition;
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
