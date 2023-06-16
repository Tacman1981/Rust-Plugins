using System;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Rust;

namespace Oxide.Plugins
{
    [Info("RocketDamageReduction", "YourName", "1.0.0")]
    [Description("Reduces rocket damage to other players")]

    class RocketDamageReduction : RustPlugin
    {
        private DynamicConfigFile config;
        private PluginConfig pluginConfig;

        private void Loaded()
        {
            LoadDefaultConfig();
        }

        protected override void LoadDefaultConfig()
        {
            pluginConfig = new PluginConfig
            {
                ReductionPercentage = 50f // Default reduction percentage is 50%
            };

            SaveConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                pluginConfig = Config.ReadObject<PluginConfig>();
            }
            catch (Exception ex)
            {
                PrintWarning($"Failed to load config file: {ex.Message}");
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(pluginConfig);
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            // Check if the damaged entity is a player and the attacker is a rocket
            if (entity is BasePlayer && hitInfo?.Initiator is BaseProjectile && hitInfo?.WeaponPrefab?.name == "rocket_launcher")
            {
                BasePlayer damagedPlayer = entity as BasePlayer;
                BasePlayer attacker = hitInfo.Initiator as BasePlayer;

                // Check if the damaged player is the same as the attacker
                if (damagedPlayer == attacker)
                    return;

                float originalDamage = hitInfo.damageTypes.Total();
                float reductionPercentage = pluginConfig.ReductionPercentage / 100f;
                float reducedDamage = originalDamage * reductionPercentage;

                hitInfo.damageTypes.ScaleAll(reducedDamage / originalDamage);
            }
        }

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Reduction Percentage")]
            public float ReductionPercentage { get; set; }
        }
    }
}
