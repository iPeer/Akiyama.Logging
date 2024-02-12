using Akiyama.Logging.Levels;
using Akiyama.Logging.Types;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;

namespace Akiyama.Logging.Configuration
{
    public class LoggerConfig : ICloneable
    {

        public string Name { get; private set; } = string.Empty;
        public LoggerType Type { get; private set; }
        public int CacheSize { get; private set; } = 100;
        public int CacheBypassLength { get; private set; } = 500;
        public LogLevel Level { get; private set; } = LogLevel.INFO;
        public string LineFormat { get; private set; } = "[<time>] [<level>] <name>: <message>";
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
            string dateTimeFormat = @"yyyy-MM-dd HH\:mm\:ss", string outputDirectory = "./logs/", int cacheSize = 30, int cacheBypassLength = 5000, int maxCycledFiles = 5, 
            bool consoleOutputDisabled = false, bool debugOutputDisabled = false)
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
        }

        internal void SetName(string newName, bool renameFile = false)
        {
            string _;
            try
            {
                this.SetName(newName, out _, renameFile);
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

        public object Clone()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                ms.Position = 0;
                object obj = bf.Deserialize(ms);
                return obj;
            }
        }
    }
}
