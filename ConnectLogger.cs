using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Oxide.Core;
using Rust;

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
            // Get the player's IP address without the port
            string ipAddress = player.net.connection.ipaddress.Split(':')[0];

            // Log player connection with timestamp and IP address
            string logMessage = $"[{DateTime.Now}] [CONNECT] {player.displayName} ({player.UserIDString}) connected from {ipAddress}.";
            LogToFile(logFilePath, logMessage, true);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            // Get the player's IP address without the port
            string ipAddress = player.net.connection.ipaddress.Split(':')[0];

            // If the disconnection reason is not provided, log it with a default reason
            if (string.IsNullOrEmpty(reason))
            {
                reason = "Disconnected (No reason given)";
            }

            // Log player disconnection with timestamp and IP address
            string logMessage = $"[{DateTime.Now}] [DISCONNECT] {player.displayName} ({player.UserIDString}) disconnected from {ipAddress}. Reason: {reason}";
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

        [ChatCommand("lastseen")]
        private string GetLastSeenMessage(string targetUserID)
{
    BasePlayer targetPlayer = null;
    foreach (BasePlayer player in BasePlayer.activePlayerList)
    {
        if (player.UserIDString == targetUserID)
        {
            targetPlayer = player;
            break;
        }
    }

    if (targetPlayer != null)
    {
        return $"Player is currently online.";
    }

    DateTime lastSeenTime = DateTime.MinValue;
    string lastSeenMessage = "This player has not logged in this wipe.";

    if (File.Exists(logFilePath))
    {
        string[] logLines = File.ReadAllLines(logFilePath);

        foreach (string logLine in logLines)
        {
            if (logLine.Contains($"({targetUserID})") && logLine.Contains("[DISCONNECT]"))
            {
                int startIndex = logLine.IndexOf('[') + 1;
                int endIndex = logLine.IndexOf(']');
                if (startIndex >= 0 && endIndex > startIndex)
                {
                    string dateString = logLine.Substring(startIndex, endIndex - startIndex);
                    DateTime logTime;
                    if (DateTime.TryParse(dateString, out logTime))
                    {
                        if (logTime > lastSeenTime)
                        {
                            lastSeenTime = logTime;
                        }
                    }
                }
            }
        }

        if (lastSeenTime != DateTime.MinValue)
        {
            TimeSpan timeSinceLastSeen = DateTime.Now - lastSeenTime;
            int hours = (int)timeSinceLastSeen.TotalHours;
            int minutes = (int)timeSinceLastSeen.TotalMinutes % 60;

            if (hours > 0)
                lastSeenMessage = $"Last seen: {hours} hour(s) and {minutes} minute(s) ago.";
            else
                lastSeenMessage = $"Last seen: {minutes} minute(s) ago.";
        }
    }
    else
    {
        lastSeenMessage = "Player logs not found.";
    }

    return lastSeenMessage;
}

    }
}
