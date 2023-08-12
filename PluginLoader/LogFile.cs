using System.IO;
using VRage.Utils;
using NLog;
using NLog.Config;
using NLog.Layouts;

namespace avaness.PluginLoader
{
    public static class LogFile
    {
        private const string fileName = "loader.log";
        private static Logger logger;
        private static LogFactory logFactory;

        public static void Init(string mainPath)
        {
            string file = Path.Combine(mainPath, fileName);
            LoggingConfiguration config = new LoggingConfiguration();
            config.AddRuleForAllLevels(new NLog.Targets.FileTarget() 
            { 
                DeleteOldFileOnStartup = true,
                FileName = file,
                Layout = new SimpleLayout("${longdate} [${level:uppercase=true}] (${threadid}) ${message:withexception=true}")
            });
            logFactory = new LogFactory(config);
            logFactory.ThrowExceptions = false;
            
            try
            {
                logger = logFactory.GetLogger("PluginLoader");
            }
            catch
            {
                logger = null;
            }
        }

        public static void Error(string text, bool gameLog = true)
        {
            WriteLine(text, LogLevel.Error, gameLog);
        }

        public static void Warn(string text, bool gameLog = true)
        {
            WriteLine(text, LogLevel.Warn, gameLog);
        }

        public static void WriteLine(string text, LogLevel level = null, bool gameLog = true)
        {
            try
            {
                if (level == null)
                    level = LogLevel.Info;
                logger?.Log(level, text);
                if(gameLog)
                    MyLog.Default?.WriteLine($"[PluginLoader] [{level.Name}] {text}");
            }
            catch 
            {
                Dispose();
            }
        }


        public static void Dispose()
        {
            if (logger == null)
                return;

            try
            {
                logFactory.Flush();
                logFactory.Dispose();
            }
            catch { }
            logger = null;
            logFactory = null;
        }
    }
}