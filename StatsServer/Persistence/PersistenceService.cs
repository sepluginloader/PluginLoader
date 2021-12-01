using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using avaness.StatsServer.Tools;
using Microsoft.Extensions.Logging;

// ReSharper disable TemplateIsNotCompileTimeConstantProblem

namespace avaness.StatsServer.Persistence
{
    public class PersistenceService : PeriodicTimerService
    {
        private readonly StatsDatabase statsDatabase;

        public PersistenceService(ILogger<PersistenceService> logger) : base(logger)
        {
            Name = GetType().Name;
            Period = Config.SavePeriod;

            statsDatabase = new StatsDatabase();
        }

#pragma warning disable CA1816
        public override void Dispose()
        {
            statsDatabase.Dispose();

            base.Dispose();
        }
#pragma warning restore CA1816

        protected override void DoWork(object state)
        {
            statsDatabase?.Save();
            Backup();
        }

        private void Backup()
        {
            // Daily backup archive file
            var zipPath = Path.Combine(Config.BackupDir, $"PluginLoaderStatsData.{Tools.Tools.FormatDateIso8601(DateTime.Today)}.zip");

            // Make
            if (File.Exists(zipPath))
                return;

            Directory.CreateDirectory(Config.BackupDir);

            try
            {
                using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                var dataDir = Config.DataDir;
                foreach (var filePath in Directory.EnumerateFiles(dataDir, "*", SearchOption.AllDirectories))
                {
                    var relativePath = filePath[dataDir.Length..];
                    zip.CreateEntryFromFile(filePath, relativePath, CompressionLevel.Optimal);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to create database backup archive {zipPath} from data directory {Config.DataDir}");
                try
                {
                    File.Delete(zipPath);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}