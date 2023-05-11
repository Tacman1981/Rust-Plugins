using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Compost Stacks", "Tacman", "1.8.0")]
    [Description("Toggle the CompostEntireStack boolean on load and for new Composter entities, which will compost entire stacks of all compostable items.")]
    public class CompostStacks : RustPlugin
    {
        private bool CompostEntireStack = true;

        private void OnServerInitialized()
        {
            UpdateComposters();
        }

        private void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity is Composter)
            {
                Composter composter = entity as Composter;
                composter.CompostEntireStack = CompostEntireStack;
            }
        }

        private void UpdateComposters()
        {
            foreach (Composter composter in BaseNetworkable.serverEntities.Where(x => x is Composter))
            {
                composter.CompostEntireStack = CompostEntireStack;
            }
        }
    }
}
