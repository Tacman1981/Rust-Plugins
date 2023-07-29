using System;
using System.IO;
using Oxide.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("Connect Logger", "Tacman", "1.0.0")]
    class ConnectLogger : RustPlugin
    {
        private string logFilePath;

        private void Init()
        {
            logFilePath = Path.Combine(Interface.Oxide.LogDirectory, "ConnectLogs.txt");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            // Log player connection with timestamp
            string logMessage = $"[{DateTime.Now}] [CONNECT] {player.displayName} ({player.UserIDString}) connected.";
            LogToFile(logFilePath, logMessage);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // Log player disconnection with timestamp
            string logMessage = $"[{DateTime.Now}] [DISCONNECT] {player.displayName} ({player.UserIDString}) disconnected. Reason: {reason}";
            LogToFile(logFilePath, logMessage);
        }

        private void LogToFile(string filePath, string message)
        {
            using (StreamWriter streamWriter = new StreamWriter(filePath, true))
            {
                streamWriter.WriteLine(message);
            }
        }
    }
}
