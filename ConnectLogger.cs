using System;
using System.Collections.Generic;
using System.IO;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Connect Logger", "Tacman", "1.4.0")]
    class ConnectLogger : RustPlugin
    {
        private string logFilePath;

        private void Init()
        {
            logFilePath = Path.Combine(Interface.Oxide.LogDirectory, "ConnectLogs.txt");
        }

        private void OnPlayerConnected(BasePlayer player)
        {
            // Log player connection with timestamp and user ID
            string logMessage = $"[{DateTime.Now}] [CONNECT] {player.displayName} ({player.UserIDString}) connected.";
            LogToFile(logFilePath, logMessage, true);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // If the disconnection reason is not provided, log it with a default reason
            if (string.IsNullOrEmpty(reason))
            {
                reason = "Disconnected (No reason given)";
            }

            // Log player disconnection with timestamp
            string logMessage = $"[{DateTime.Now}] [DISCONNECT] {player.displayName} ({player.UserIDString}) disconnected. Reason: {reason}";
            LogToFile(logFilePath, logMessage, true);
        }

        private void LogToFile(string filePath, string message, bool reverseOrder)
        {
            if (reverseOrder)
            {
                // Reverse the log order and append the new log message
                List<string> logLines = new List<string>();

                if (File.Exists(filePath))
                {
                    string[] existingLogContent = File.ReadAllLines(filePath);
                    logLines.AddRange(existingLogContent);
                }

                logLines.Insert(0, message);

                // Write the updated log content back to the file
                File.WriteAllLines(filePath, logLines);
            }
            else
            {
                // Append the new log message at the end of the log file
                using (StreamWriter streamWriter = new StreamWriter(filePath, true))
                {
                    streamWriter.WriteLine(message);
                }
            }
        }

        private void OnNewSave(string filename)
        {
            // Delete the log file on a new server save (wipe)
            if (File.Exists(logFilePath))
            {
                File.Delete(logFilePath);
            }
        }
    }
}
