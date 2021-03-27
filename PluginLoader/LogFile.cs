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
            writer = File.CreateText(file);
        }

        public static void WriteLine(string text, bool gameLog = true)
        {
            writer?.WriteLine($"{DateTime.Now} {text}");
            if(gameLog)
                MyLog.Default.WriteLine($"[PluginLoader] {text}");
        }

        public static void Dispose()
        {
            if (writer == null)
                return;

            writer.Flush();
            writer.Close();
            writer = null;
        }

        public static void Flush()
        {
            writer.Flush();
        }
    }
}
