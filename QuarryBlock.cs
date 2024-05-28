using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("QuarryBlock", "Tacman", "1.0.0")]
    [Description("Disables placement of Quarry Blocks on the server.")]
    public class QuarryBlock : RustPlugin
    {
        private void OnServerInitialized()
        {
            permission.RegisterPermission("quarryblock.bypass", this);
        }

        private object CanBuild(Planner planner, Construction prefab)
        {
            if (prefab.fullName.Contains("quarry"))
            {
                var player = planner.GetOwnerPlayer();
                if (player != null && !permission.UserHasPermission(player.UserIDString, "quarryblock.bypass"))
                {
                    player.ChatMessage("This is no longer allowed, try /quarry to use them");
                    return false;
                }
            }

            return null;
        }
    }
}
