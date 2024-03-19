using Akiyama.Logging.Configuration;
using Akiyama.Logging.Levels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Akiyama.Logging.Loggers
{
    public class Logger
    {

        /// <summary>
        /// The <see cref="LoggerConfig"/> object for this <see cref="Logger"/>
        /// </summary>
        public readonly LoggerConfig Config;
        private readonly List<string> LineCache = new List<string>();

        /// <summary>
        /// Returns the instance corresponding to the parent of this <see cref="Logger"/>. If this Logger is not a child, or has no parent, then it returns <see cref="null"/>.
        /// </summary>
        public Logger Parent
        {
            get
            {
                return this.Config.Parent;
            }
        }

        private readonly List<Logger> _children = new List<Logger>();
        /// <summary>
        /// Returns a read-only list of Children for this <see cref="Logger"/>, if any.
        /// </summary>
        public ReadOnlyCollection<Logger> Children
        {
            get
            {
                return this._children.AsReadOnly();
            }
        }

        public Logger(LoggerConfig config)
        {
            this.Config = config;
            this.TrySetUpColourConsole();
            this.CycleFiles();
        }

        private void CycleFiles()
        {
            if (this.Config.MaxCycledFiles == 0)
            {
                if (File.Exists(this.Config.LogPath))
                    File.Delete(this.Config.LogPath);
            }
            else if (this.Config.MaxCycledFiles == 1)
            {
                if (File.Exists(Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.previous{Path.GetExtension(this.Config.LogPath)}")))
                {
                    File.Delete(Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.previous{Path.GetExtension(this.Config.LogPath)}"));
                }
                if (File.Exists(this.Config.LogPath))
                    File.Move(this.Config.LogPath, Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.previous{Path.GetExtension(this.Config.LogPath)}"));
            }
            else
            {
                for (int x = this.Config.MaxCycledFiles; x > 0; x--)
                {
                    string fileName = Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.{x}{Path.GetExtension(this.Config.LogPath)}");
                    if (x == this.Config.MaxCycledFiles)
                    {
                        if (File.Exists(fileName))
                            File.Delete(fileName);
                        continue;
                    }

                    if (!File.Exists(fileName))
                        continue;
                    if (File.Exists(fileName))
                    {
                        string newPath = Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.{x + 1}{Path.GetExtension(this.Config.LogPath)}");
                        File.Move(fileName, newPath);
                    }

                }
                if (File.Exists(this.Config.LogPath))
                {
                    File.Move(this.Config.LogPath, Path.Combine(this.Config.LogDirectory, $"{Path.GetFileNameWithoutExtension(this.Config.LogPath)}.1{Path.GetExtension(this.Config.LogPath)}"));
                }
            }
        }

        private void TrySetUpColourConsole()
        {
            if (this.Config.Formatter != LogFormatter.COLOUR) return;
#if (WINDOWS || NETFRAMEWORK)
            var handle = GetStdHandle(STD_OUTPUT_HANDLE);
            bool ready = true;
            if (!GetConsoleMode(handle, out uint mode)) { this.Config.Formatter = LogFormatter.DEFAULT; ready = false; }
            mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            if (!SetConsoleMode(handle, mode)) { this.Config.Formatter = LogFormatter.DEFAULT; ready = false; }
            if (!ready) this.LogInternal("Couldn't set up console to allow colouring.");
#endif
            // Is there even a way to detect this shit on *nix?
        }
#if (WINDOWS || NETFRAMEWORK)
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);
        [DllImport("kernel32.dll")]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
#endif
        /// <summary>
        /// Creates a child of this <see cref="Logger"/>, using this Logger's config as a base.
        /// </summary>
        /// <param name="childName">The Name of the child <see cref="Logger"/>.</param>
        /// <param name="shareFile">If <b>true</b>, this child will share an output with with it's parent <see cref="Logger"/>. <b>False</b> will mean this child logs to its own output file.</param>
        /// <returns></returns>
        public TSource CreateChild<TSource>(string childName, bool shareFile = false)
        {
            LoggerConfig config = (LoggerConfig)Config.Clone();
            config.SetAsChild(parent: this, shareFile: shareFile);
            config.SetName($"{config.Name}.{childName}");
            if (!shareFile) { config.UpdateLogPath(); }
            TSource child = (TSource)Activator.CreateInstance(typeof(TSource), config);
            this._children.Add(item: child as Logger);
            return child;
        }

        public void SetName(string name, bool keepFile = false)
        {
            try
            {
                this.Config.SetName(name, out string oldName, keepFile);
                this.LogInternal($"Logger name was changed to '{name}' (was '{oldName}').");
            }
            catch (Exception ex)
            {
                this.Error($"Logger rename failed to complete:", ex);
            }
        }

        public void SetLevel(LogLevel level)
        {
            this.Config.SetLevel(level);
            this.LogInternal($"Log level changed to '{level}'.");
        }

        [Conditional("BEBUG")]
        public void SetDebugOutputStates(OutputState console = OutputState.ENABLED, OutputState debug = OutputState.ENABLED)
        {
            this.Config.SetDebutOutputStates(console, debug);
        }

        private void WriteCachedLinesToFile()
        {
            if (this.LineCache.Count > 0)
            {
                List<string> cache = this.LineCache.ToList();
                this.LineCache.Clear();
                using (StreamWriter w = new StreamWriter(this.Config.LogPath, append: true, encoding: this.Config.Encoding))
                {
                    w.Write(string.Join("\n", cache) + "\n");
                }
                cache.Clear();
            }
        }

        internal void AddLineToCache(string line, int rawStringLength)
        {
            // This is very similar to the old version (because it stills works for what we're doing here)
            this.LineCache.Add(line);
            if (this.Config.Type == Types.LoggerType.REALTIME || this.LineCache.Count >= this.Config.CacheSize || rawStringLength >= this.Config.CacheBypassLength)
            {
                this.WriteCachedLinesToFile();
            }
        }

        internal void LogInternal(string message, params object[] fillers) => Log(message, (LogLevel)999, fillers);
        public void Info(string message, params object[] fillers) => Log(message, LogLevel.INFO, fillers);
        public void Log(string message, params object[] fillers) => Log(message, LogLevel.INFO, fillers);
        public void Warning(string message, params object[] fillers) => Log(message, LogLevel.WARNING, fillers);
        public void Debug(string message, params object[] fillers) => Log(message, LogLevel.DEBUG, fillers);
        public void Error(string message, Exception exception, params object[] fillers) => Error($"{message}\n{exception.Message}\n{exception.StackTrace}", fillers);
        public void Error(Exception exception) => Error($"{exception.Message}\n{exception.StackTrace}", new object[0]);
        public void Error(string message, params object[] fillers) => Log(message, LogLevel.ERROR, fillers);
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="level">The <see cref="LogLevel"/> to log this message as.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=netframework-4.7.2"/>.</param>
        protected void Log(string str, LogLevel level, params object[] fillers)
        {

            if (level < this.Config.Level) { return; }

            string msg = string.Format(str, fillers);
            string prefix = NameFormat(level);

            string time = DateTime.Now.ToString(this.Config.DateTimeFormat);

            string line = this.Config.LineFormat.Replace("<time>", time)
                                                .Replace("<level>", prefix)
                                                .Replace("<name>", this.Config.Name)
                                                .Replace("<message>", msg);
            //line = string.Format("[{0}] [{1}] {2}: {3}", time, prefix.PadRight(7, ' '), this.Config.Name, msg);

            if (!this.Config.DebugOutputDisabled && Debugger.IsAttached) { System.Diagnostics.Debug.WriteLine(Regex.Replace(line, "\u001b\\[\\d{1,2}m", "")); }
            if (!this.Config.ConsoleOutputDisabled) { Console.WriteLine(line); }


            if (this.Config.Formatter == LogFormatter.COLOUR) { line = Regex.Replace(line, "\u001b\\[\\d{1,2}m", ""); }
            if (this.Config.IsChild && this.Config.ShareFileWithParent) { this.Parent.AddLineToCache(line, msg.Length); }
            else { this.AddLineToCache(line, msg.Length); }

        }


        private string NameFormat(LogLevel level) // This is horrible lol
        {
            string prefix = level.ToString().ToUpper();
            if (this.Config.Formatter == LogFormatter.DEFAULT)
            {
                switch (level)
                {
                    case LogLevel.ALWAYS:
                    case LogLevel.VERBOSE:
                    case LogLevel.INFO:
                        prefix = "INFO";
                        break;
                    case LogLevel.WARNING:
                        prefix = "WARNING";
                        break;
                    case LogLevel.ERROR:
                        prefix = "ERROR";
                        break;
                    case LogLevel.DEBUG:
                        prefix = "DEBUG";
                        break;
                    default:
                        prefix = "-------";
                        break;
                }
            }
            else if (this.Config.Formatter == LogFormatter.EMOJI)
            {
                switch (level)
                {
                    case LogLevel.ALWAYS:
                    case LogLevel.VERBOSE:
                        return "💭";
                    case LogLevel.INFO:
                        return "ℹ️";
                    case LogLevel.WARNING:
                        return "⚠️";
                    case LogLevel.ERROR:
                        return "❌";
                    case LogLevel.DEBUG:
                        return "🔵";
                    default:
                        return "--";
                }
            }
            else if (this.Config.Formatter == LogFormatter.COLOUR)
            {
                switch (level)
                {
                    case LogLevel.ALWAYS:
                    case LogLevel.VERBOSE:
                    case LogLevel.INFO:
                        return $"\x1b[34m{prefix,-7}\x1b[0m";
                    case LogLevel.WARNING:
                        return $"\x1b[33m{prefix,-7}\x1b[0m";
                    case LogLevel.ERROR:
                        return $"\x1b[31m{prefix,-7}\x1b[0m";
                    case LogLevel.DEBUG:
                        return $"\x1b[35m{prefix,-7}\x1b[0m";
                    default:
                        return "-------";
                }
            }
            return prefix.PadRight(7, ' ');
        }

    }

}
