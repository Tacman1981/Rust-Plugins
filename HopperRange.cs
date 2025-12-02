//this is the simple version of Simple Hopper Controller
using Oxide.Core;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("HopperRange", "Tacman", "1.0.0")]
    [Description("Adjusts the collection range of hoppers by resizing their ItemTrigger SphereCollider and resets on unload")]
    public class HopperRange : RustPlugin
    {
        #region Range Settings
        private float CustomRange = 10f;
        private float RevertTo = 3f;
        #endregion

        #region Hooks
        void OnServerInitialized()
        {
            Puts($"Applying configured radius ({CustomRange}) to all existing hoppers.");
            int count = 0;
            //Find existing hoppers and apply their radius
            foreach (var hopper in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
            {
                if (hopper?.ShortPrefabName != "hopper.deployed")
                    continue;

                if (ApplyRange(hopper))
                    count++;
            }

            Puts($"Adjusted {count} existing hoppers.");
        }

        object OnEntitySpawned(BaseEntity entity)
        {
            //Updating the radius on placement of the hopper
            if (entity?.ShortPrefabName == "hopper.deployed")
            {
                timer.Once(0.5f, () => ApplyRange(entity));
            }
            return null;
        }

        void Unload()
        {
            Puts("Resetting all hoppers to default ItemTrigger SphereCollider radius.");
            int count = 0;
            //Resetting the radius when the plugin unloads
            foreach (var hopper in UnityEngine.Object.FindObjectsOfType<BaseEntity>())
            {
                if (hopper?.ShortPrefabName != "hopper.deployed")
                    continue;

                if (ResetToDefault(hopper))
                    count++;
            }

            Puts($"Reset {count} hoppers.");
        }
        #endregion

        #region Helpers
        private bool ApplyRange(BaseEntity hopper)
        {
            if (hopper == null) return false;
            //Looking for the ItemTrigger to get the SphereCollider from it
            var itemTrigger = FindTrigger(hopper.gameObject.transform, "ItemTrigger");
            if (itemTrigger == null)
            {
                Puts($"[WARN] No ItemTrigger child found on hopper at {hopper.transform.position}");
                return false;
            }
            //If the trigger is found, look for the SphereCollider
            var sphere = itemTrigger.GetComponent<SphereCollider>();
            if (sphere == null)
            {
                Puts($"[WARN] No SphereCollider on ItemTrigger at {hopper.transform.position}");
                return false;
            }
            //Applying custom radius here
            sphere.isTrigger = true;
            sphere.radius = CustomRange;

            Puts($"[HopperRange] Set ItemTrigger SphereCollider radius to {sphere.radius:F2} at {hopper.transform.position}");
            return true;
        }

        private bool ResetToDefault(BaseEntity hopper)
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
            sphere.radius = RevertTo;

            Puts($"[HopperRange] Reset ItemTrigger SphereCollider radius to {sphere.radius:F2} at {hopper.transform.position}");
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

