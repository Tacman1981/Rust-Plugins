using Newtonsoft.Json;
using Oxide.Core;
using System;
using UnityEngine;
using System.Linq;

//I dont expect there is much more I can add to this simple hopper controller for free, if you want more functionality I recommend Better Hoppers Plus on Codefling.
//This contains a somehwat dynamic speed of sorts, so items should not be teleporting from mid air to your hoppers.
//Added a configurable speed option for items, it is recommended to make this setting 2x the range or higher. Thia will prevent the items teleporting before they reach the hopper.

namespace Oxide.Plugins
{
    [Info("Better Hopper Controller", "Tacman", "1.0.3")]
    [Description("Adjusts the collection range of hoppers by resizing their ItemTrigger SphereCollider and resets on unload")]
    public class BetterHopperController : RustPlugin
    {
        #region constants
        private const float baseHopperRange = 3f; //This is the default range of the hoppers, this must remain to revert to standard on plugin unload. Must remain constant.
        private const float baseItemSpeed = 2f; //This is the default speed of the items picked up by the hopper, to be used on unload. Must remain constant.
        private bool debug = false;
        #endregion

        private string usePerm = "betterhoppercontroller.use";

        #region Config
        static Configuration config;
        public class Configuration
        {
            // This uses raycast so remains a line of sight feature.
            [JsonProperty("Range your hoppers will collect from. (default 3f)")]
            public float CustomRange;
            [JsonProperty("Speed of items when picked up by hoppers (recommended at least 2x the maximum range to prevent items teleporting)")]
            public float ItemSpeed;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    CustomRange = 3f,
                    ItemSpeed = 6f
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null) LoadDefaultConfig();
                SaveConfig();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                PrintWarning("Creating new configuration file.");
                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();
        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Hooks

        private void Init()
        {
            permission.RegisterPermission(usePerm, this);
        }
        void OnServerInitialized()
        {
            Puts($"Applying configured radius ({config.CustomRange}) to all existing hoppers.");
            int count = 0;
            //Find existing hoppers and apply their radius and speed. Optimized here a little bit
            foreach (var hopper in BaseNetworkable.serverEntities.OfType<Hopper>())
            {
                if (!permission.UserHasPermission(hopper.OwnerID.ToString(), usePerm)) continue;
                if (ApplyChanges(hopper))
                    count++;
            }

            Puts($"Adjusted {count} existing hoppers.");
        }

        object OnEntitySpawned(Hopper hopper)
        {
            //Updating the radius on placement of the hopper
            if (hopper?.ShortPrefabName == "hopper.deployed")
            {
                if (permission.UserHasPermission(hopper.OwnerID.ToString(), usePerm))
                    timer.Once(0.5f, () => ApplyChanges(hopper));
            }
            return null;
        }

        void Unload()
        {
            Puts("Resetting all hoppers to default ItemTrigger SphereCollider radius.");
            int count = 0;
            //Resetting the radius and speed when the plugin unloads
            foreach (var hopper in BaseNetworkable.serverEntities.OfType<Hopper>())
            {
                if (ResetToDefault(hopper))
                    count++;
            }

            Puts($"Reset {count} hoppers.");
        }
        #endregion

        #region Helpers
        private bool ApplyChanges(Hopper hopper)
        {
            if (hopper == null) return false;
            //Looking for the ItemTrigger to get the SphereCollider from it
            var itemTrigger = FindTrigger(hopper.gameObject.transform, "ItemTrigger");
            if (itemTrigger == null)
            {
                Puts($"[WARN] No ItemTrigger child found on hopper at {hopper.transform.position}");
                return false;
            }
            //If the trigger is found, look for the SphereCollider, shouldnt really be null if it is a hopper but check for null anyway.
            var sphere = itemTrigger.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                Puts($"[WARN] No SphereCollider on ItemTrigger at {hopper.transform.position}");
                return false;
            }
            sphere.isTrigger = true;
            sphere.radius = config.CustomRange;
            hopper.ItemMoveSpeed = config.ItemSpeed;
            if (debug) 
                Puts($"[HopperRange] Set ItemTrigger SphereCollider radius to {sphere.radius:F2} and Item Move speed to {hopper.ItemMoveSpeed:F2} at {hopper.transform.position}");
            return true;
        }

        private bool ResetToDefault(Hopper hopper)
        {
            if (hopper == null) return false;

            var itemTrigger = FindTrigger(hopper.gameObject.transform, "ItemTrigger");
            if (itemTrigger == null)
            {
                Puts($"[WARN] No ItemTrigger child found on hopper at {hopper.transform.position}");
                return false;
            }

            var sphere = itemTrigger.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                Puts($"[WARN] No SphereCollider on ItemTrigger at {hopper.transform.position}");
                return false;
            }

            sphere.isTrigger = true;
            sphere.radius = baseHopperRange;
            hopper.ItemMoveSpeed = baseItemSpeed;
            if (debug)
                Puts($"[HopperRange] Reset ItemTrigger SphereCollider radius to {sphere.radius:F2} and Item Speed set to {baseItemSpeed} at {hopper.transform.position}");
            return true;
        }

        private Transform FindTrigger(Transform parent, string name)
        {
            //Finding all of the hoppers, so it can look for their trigger and eventually the SphereCollider that controls pickup range.
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                var found = FindTrigger(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }
        #endregion
    }
}
