using System;
using System.IO;
using VRage.Utils;

namespace avaness.PluginLoader
{
    public static class LogFile
    {
        private const string fileName = "loader.log";
        private static StreamWriter writer;

        public static void Init(string mainPath)
        {
            string file = Path.Combine(mainPath, fileName);
            try
            {
                writer = File.CreateText(file);
            }
            catch
            {
                writer = null;
            }
        }

        /// <summary>
        /// Writes the specifed text to the log file.
        /// WARNING: Not thread safe!
        /// </summary>
        public static void WriteLine(string text, bool gameLog = true)
        {
            try
            {
                writer?.WriteLine($"{DateTime.UtcNow:O} {text}");
                if (gameLog)
                    WriteGameLog(text);
                writer?.Flush();
            }
            catch 
            {
                Dispose();
            }
        }

        /// <summary>
        /// Writes the specifed text to the game log file.
        /// This function is thread safe.
        /// </summary>
        public static void WriteGameLog(string text)
        {
            MyLog.Default.WriteLine($"[PluginLoader] {text}");
        }

        public static void WriteTrace(string text, bool gameLog = true)
        {
#if DEBUG
            writer?.WriteLine($"{DateTime.UtcNow:O} {text}");
            if(gameLog)
                LogFile.WriteGameLog($"[PluginLoader] {text}");
            writer?.Flush();
#endif
        }

        public static void Dispose()
        {
            if (writer == null)
                return;

            try
            {
                writer.Flush();
                writer.Close();
            }
            catch { }
            writer = null;
        }
    }
}