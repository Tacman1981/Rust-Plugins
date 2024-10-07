using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using System;

namespace Oxide.Plugins
{
    [Info("BradleyCrateMove","Tacman","1.0.0")]
    public class BradleyCrateMove : RustPlugin
    {
        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.ShortPrefabName == "bradley_crate")
            {
                Vector3 originalPosition = entity.transform.position; // Store original position
                entity.transform.position += new Vector3(0, 10f, 0); // Adjust position
                Vector3 difference = entity.transform.position - originalPosition; // Calculate difference
                Console.WriteLine($"Transformed position of {entity.ShortPrefabName} by {difference}");
            }
        }
    }
}
