using System;
using System.IO;

namespace avaness.StatsServer
{
    public static class Config
    {
        private static readonly string UserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        public static readonly string BackupDir;
        public static readonly string DataDir;
        public static readonly int SavePeriod;

        static Config()
        {
            BackupDir = Environment.GetEnvironmentVariable("PL_BACKUP_DIR")
                      ?? Path.Combine(UserDir, ".StatsServer", "Backup");

            DataDir = Environment.GetEnvironmentVariable("PL_DATA_DIR")
                      ?? Path.Combine(UserDir, ".StatsServer", "Data");

            var periodText = Environment.GetEnvironmentVariable("PL_SAVE_PERIOD");
            SavePeriod = int.TryParse(periodText, out var period) ? period : 10;
        }
    }
}