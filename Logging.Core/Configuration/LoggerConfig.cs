using Akiyama.Logging.Levels;
using Akiyama.Logging.Loggers;
using Akiyama.Logging.Types;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
#if NET5_0_OR_GREATER
using System.Text.Json;
#endif

namespace Akiyama.Logging.Configuration
{
    public class LoggerConfig
    {
        public string Name { get; internal set; } = string.Empty;
        public LoggerType Type { get; private set; }
        public int CacheSize { get; private set; } = 100;
        public int CacheBypassLength { get; private set; } = 500;
        public LogLevel Level { get; private set; } = LogLevel.INFO;
        public string LineFormat { get; private set; } = "[<time>] [<level>] <name>: <message>";
        public LogFormatter Formatter { get; internal set; } = LogFormatter.DEFAULT_COLOUR;
        public string DateTimeFormat { get; private set; } = @"yyyy-MM-dd HH\:mm\:ss";
        public string LogDirectory { get; private set; }
        public string LogPath { get; private set; }
        public int MaxCycledFiles { get; private set; } = 5;
        public bool ConsoleOutputDisabled { get; private set; }
        public bool DebugOutputDisabled { get; private set; } = RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
        public bool IsChild { get; private set; } = false;
        public Logger Parent { get; private set; } = null;
        public bool ShareFileWithParent { get; private set; } = false;

        public LoggerConfig(string name, LoggerType type = LoggerType.CACHED, LogLevel level = LogLevel.INFO, string lineFormat = "[<time>] [<level>] <name>: <message>",
            LogFormatter formatter = LogFormatter.DEFAULT_COLOUR, string dateTimeFormat = @"yyyy-MM-dd HH\:mm\:ss", string outputDirectory = "./logs/",
            int cacheSize = 30, int cacheBypassLength = 5000, int maxCycledFiles = 5, bool consoleOutputDisabled = false, bool debugOutputDisabled = false)
        {
            this.Name = name;
            this.Type = type;
            this.CacheSize = cacheSize;
            this.CacheBypassLength = cacheBypassLength;
            this.Level = level;
            this.LogDirectory = outputDirectory;
            this.LogPath = Path.Combine(this.LogDirectory, $"{this.Name}.log");
            this.MaxCycledFiles = maxCycledFiles;
            this.ConsoleOutputDisabled = consoleOutputDisabled;
            this.DebugOutputDisabled = debugOutputDisabled;
            this.DateTimeFormat = dateTimeFormat;
            this.LineFormat = lineFormat;
            this.Formatter = formatter;
            this.MakeDirectories();
        }

        internal void SetName(string newName, bool renameFile = false)
        {
            try
            {
                this.SetName(newName, out string _, renameFile);
            }
            catch
            {
                throw;
            }
        }

        internal void SetName(string newName, out string oldName, bool renameFile = false)
        {
            oldName = this.Name;
            if (renameFile && !this.IsChild)
            {
                string oldPath = this.LogPath;
                this.UpdateLogPath();
                try
                {
                    if (File.Exists(this.LogPath))
                    {
                        File.Move(this.LogPath, this.LogPath);
                    }
                }
                catch
                {
                    this.Name = oldName;
                    this.LogPath = oldPath;
                    throw;
                }

            }
            this.Name = newName;
        }

        internal void UpdateLogPath()
        {
            this.LogPath = Path.Combine(this.LogDirectory, $"{this.Name}.log");
        }

        internal void MakeDirectories()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(this.LogDirectory));
        }

        internal void SetLevel(LogLevel level)
        {
            this.Level = level;
        }

        internal void SetDebutOutputStates(OutputState console, OutputState debug)
        {
            this.DebugOutputDisabled = debug == OutputState.DISABLED;
            this.ConsoleOutputDisabled = console == OutputState.DISABLED;
        }

        internal void SetAsChild(Logger parent, bool shareFile = false)
        {
            this.IsChild = true;
            this.Parent = parent;
            this.ShareFileWithParent = shareFile;
        }


        public LoggerConfig Clone()
        {
#if NET5_0_OR_GREATER
            string _json = JsonSerializer.Serialize(this);
            return JsonSerializer.Deserialize<LoggerConfig>(json: _json);
#else

            var serializer = new DataContractSerializer(this.GetType());
            using (MemoryStream  ms = new MemoryStream())
            {
                serializer.WriteObject(ms, this);
                ms.Position = 0;
                return (LoggerConfig)serializer.ReadObject(ms);
            }
#endif
        }
    }
}
