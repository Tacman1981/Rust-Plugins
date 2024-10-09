using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System;

// Made with the intention of fixing the spawn issue with crates sometimes being pushed under terrain by heli or bradley gibs.
namespace Oxide.Plugins
{
    [Info("BradleyCrateMove","Tacman","1.0.1")]
    public class BradleyCrateMove : RustPlugin
    {
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.ShortPrefabName == "bradley_crate" || entity.ShortPrefabName == "heli_crate")
            {
                Vector3 originalPosition = entity.transform.position; // Store original position
                entity.transform.position += new Vector3(0, 10f, 0); // Adjust position
                Vector3 difference = entity.transform.position - originalPosition; // Calculate difference
                //Console.WriteLine($"Transformed position of {entity.ShortPrefabName} by {difference}");
            }
        }
    }
}
