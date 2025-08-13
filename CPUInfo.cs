using Oxide.Core.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CPUInfo", "Tacman", "0.0.5")]
    [Description("Prints server CPU and RAM info to console or RCON")]
    public class CPUInfo : CovalencePlugin
    {
        [Command("cpuinfo")]
        private void CmdCpuInfo(IPlayer player, string command, string[] args)
        {
            player.Message($"CPU: {SystemInfo.processorType}");
            player.Message($"Cores: {SystemInfo.processorCount}");
            player.Message($"Clock Speed: {SystemInfo.processorFrequency} MHz");
            player.Message($"RAM: {SystemInfo.systemMemorySize / 1024f:F2} GB");
        }
    }
}
