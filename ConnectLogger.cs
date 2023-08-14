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

        private void OnNewSave()
        {
            ClearLogFile(); // Clear the log file on a new map
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            // Log player connection with timestamp
            string logMessage = $"[{DateTime.Now}] [CONNECT] {player.displayName} ({player.UserIDString}) connected.";
            PrependLogToFile(logFilePath, logMessage); // Use PrependLogToFile instead of LogToFile
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // Log player disconnection with timestamp
            string logMessage = $"[{DateTime.Now}] [DISCONNECT] {player.displayName} ({player.UserIDString}) disconnected. Reason: {reason}";
            PrependLogToFile(logFilePath, logMessage); // Use PrependLogToFile instead of LogToFile
        }

        private void PrependLogToFile(string filePath, string message)
        {
            // Read the existing content of the file
            string existingContent = File.ReadAllText(filePath);

            // Combine the new message with the existing content
            string newContent = message + Environment.NewLine + existingContent;

            // Write the combined content back to the file
            File.WriteAllText(filePath, newContent);
        }

        private void ClearLogFile()
        {
            // Delete the log file if it exists
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
    }
}
