﻿using Akiyama.Logging.Configuration;
using Akiyama.Logging.Levels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Akiyama.Logging
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
        }

        /// <summary>
        /// Creates a child of this <see cref="Logger"/>, using this Logger's config as a base.
        /// </summary>
        /// <param name="childName">The Name of the child <see cref="Logger"/>.</param>
        /// <param name="shareFile">If <b>true</b>, this child will share an output with with it's parent <see cref="Logger"/>. <b>False</b> will mean this child logs to its own output file.</param>
        /// <returns></returns>
        public Logger CreateChild(string childName, bool shareFile = false)
        {
            LoggerConfig config = (LoggerConfig)Config.Clone();
            config.SetAsChild(parent: this, shareFile: shareFile);
            config.SetName($"{config.Name}.{childName}");
            if (!shareFile) { config.UpdateLogPath(); }
            Logger child = new Logger(config);
            this._children.Add(child);
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
                using (StreamWriter w = new StreamWriter(this.Config.LogPath, append: true, encoding: Encoding.UTF8))
                {
                    w.Write(string.Join("\n", cache)+"\n");
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
        private void Log(string str, LogLevel level, params object[] fillers)
        {

            if (level < this.Config.Level) { return; }

            string msg = string.Format(str, fillers);
            string prefix = "INFO";
            switch (level)
            {
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

            string time = DateTime.Now.ToString(this.Config.DateTimeFormat);

            string line = this.Config.LineFormat.Replace("<time>", time)
                                                .Replace("<level>", prefix.PadRight(7, ' '))
                                                .Replace("<name>", this.Config.Name)
                                                .Replace("<message>", msg);
            //line = string.Format("[{0}] [{1}] {2}: {3}", time, prefix.PadRight(7, ' '), this.Config.Name, msg);
#if DEBUG
            if (!this.Config.DebugOutputDisabled && Debugger.IsAttached) { System.Diagnostics.Debug.WriteLine(line); }
            if (!this.Config.ConsoleOutputDisabled) { Console.WriteLine(line); }
#endif
            if (this.Config.IsChild && this.Config.ShareFileWithParent) { this.Parent.AddLineToCache(line, msg.Length); }
            else { this.AddLineToCache(line, msg.Length); }

        }
    }

}
