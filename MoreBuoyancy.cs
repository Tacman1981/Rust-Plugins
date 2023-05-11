using UnityEngine;
using Rust;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Oxide.Plugins
{
    [Info("Buoyancy", "Tacman", "1.4.0")]
    [Description("Makes helicopter and code locked hackable crates buoyant")]
    class MoreBuoyancy : RustPlugin
    {
        #region Config
        public PluginConfig _config;

        public PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                DetectionRate = 1,
            };
        }

        public class PluginConfig
        {
            public int DetectionRate;
        }
        #endregion

        #region Oxide
        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity == null ||
    (entity.ShortPrefabName != "heli_crate" ||
     entity.ShortPrefabName != "codelockedhackablecrate" ||
     entity.ShortPrefabName != "supply_drop" ||
     !entity.ShortPrefabName.Contains("minicopter") ||
     !entity.ShortPrefabName.Contains("scraptransporthelicopter")))
            {
                return;
            }
            MakeBuoyant buoyancy = entity.gameObject.AddComponent<MakeBuoyant>();
            buoyancy.buoyancyScale = 3f;
            buoyancy.detectionRate = _config.DetectionRate;
        }
        #endregion

        #region Classes
        class MakeBuoyant : MonoBehaviour
        {
            public float buoyancyScale;
            public int detectionRate;
            private BaseEntity _entity;
            private bool _hasLanded = false;

            void Awake()
            {
                _entity = GetComponent<BaseEntity>();
                if (_entity == null) Destroy(this);
            }

            void FixedUpdate()
            {
                if (_entity == null)
                {
                    Destroy(this);
                    return;
                }
                if (!_hasLanded && UnityEngine.Time.frameCount % detectionRate == 0 && WaterLevel.Factor(_entity.WorldSpaceBounds().ToBounds()) > 0.65f)
                {
                    BuoyancyComponent();
                    _hasLanded = true;
                    Invoke("RemoveParachute", 0.5f);
                }
            }

            void BuoyancyComponent()
            {
                Buoyancy buoyancy = gameObject.AddComponent<Buoyancy>();
                buoyancy.buoyancyScale = buoyancyScale;
                buoyancy.rigidBody = gameObject.GetComponent<Rigidbody>();
                buoyancy.rigidBody.velocity = UnityEngine.Vector3.zero;
                buoyancy.rigidBody.angularVelocity = UnityEngine.Vector3.zero;
                buoyancy.rigidBody.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezeRotationX;
            }

            void RemoveParachute()
            {
                if (_entity.ShortPrefabName == "supply_drop")
                {
                    SupplyDrop supplyDrop = _entity.GetComponent<SupplyDrop>();
                    if (supplyDrop != null)
                    {
                        supplyDrop.RemoveParachute();
                        supplyDrop.isLootable = true; // set isLootable to true after removing parachute
                    }
                }
            }
        }
        #endregion
    }
}
