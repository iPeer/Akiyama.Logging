using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Akiyama.Logging
{
    public class Logger : IDisposable
    {
        /// <summary>
        /// The name of this <see cref="Logger"/>.<br />This is also used as the file this log outputs to.
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// The type of this <see cref="Logger"/>. Can be either <see cref="LoggerType.REALTIME"/>, or <see cref="LoggerType.CACHED"/>.
        /// </summary>
        /// <remarks>
        /// Loggers of type <see cref="LoggerType.REALTIME"/> will output their lines in real-time to their associated log file.<br />
        /// Loggers of type <see cref="LoggerType.CACHED"/> will collect lines until either the number of saved lines matches or exceeds <see cref="Logger.CachedThreshold"/>, or until the Logger is being disassembled.
        /// <br /><br /><b>Note</b>: Due to no guarantee of disposing methods or finalisers being called on objects in NET6+, it's recommended to either use <see cref="LoggerType.REALTIME"/>, or manually call <see cref="Logger.WritePendingLines()"/> during application shutdown on those frameworks for important loggers.
        /// </remarks>
        public LoggerType Type { get; private set; }
        /// <summary>
        /// Indicates the minimum number of log lines this <see cref="Logger"/> must have in its cache before it will write them to its respective log file.<br />
        /// Lines will also be written to their respective log file when the class is being finalised.
        /// </summary>
        public int CachedThreshold { get; private set; }
        /// <summary>
        /// The current <see cref="LogLevel"/> of this <see cref="Logger"/>. Log lines with a <see cref="LogLevel"/> lower than this will not be logged.<br />
        /// Defaults to <see cref="LogLevel.INFO"/>.
        /// </summary>
        public LogLevel Level { get; private set; }
        /// <summary>
        /// The directory (relative or absolute) in which this <see cref="Logger"/>'s log file will be written.
        /// </summary>
        public string LogsDirectory { get; private set; }
        /// <summary>
        /// The complete path, including file name, to which this <see cref="Logger"/>'s log lines will be written.
        /// </summary>
        public string LogFilePath { get; private set; }
        /// <summary>
        /// Indicates the maximum number of old log files to be saved by this <see cref="Logger"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this value is 0, no log files are retained when cycling files. If the log file exists, it will be deleted.
        /// </para>
        /// <para>
        /// If this value is 1, the last log file will be renamed to <c>&lt;name&gt;.log.last</c>
        /// </para>
        /// <para>
        /// If this value is 2 or greater, log files will be cycled through incremently as <c>&lt;name&gt;.log.&lt;num&gt;</c> where <c>num</c> is an integer of increasing value up until the value of this variable.
        /// <br />If a file already exists with <c>num</c> equal to this variable, it is deleted.
        /// </para>
        /// </remarks>
        public int MaxOldFiles { get; private set; }

        /// <summary>
        /// Holds a list of cached log messages that need to be written to file.
        /// </summary>
        private readonly List<string> OutputCache = new List<string>();

        /// <summary>
        /// If <b>true</b>, disables output to <see cref="System.Console"/> when compiled in debug mode.
        /// </summary>
        private bool DebugOutputConsoleDisabled = false;
        /// <summary>
        /// If <b>true</b>, disables output to <see cref="System.Diagnostics.Debug"/> when compiled in debug mode.
        /// </summary>
        private bool DebugOutputDebugDisabled = false;

        private bool _renameInProgress = false;

        private bool Disposed = false;

        // TODO: docstring
        public Logger(string name, LoggerType type = LoggerType.REALTIME, int cacheThreshold = 30, LogLevel defaultLevel = LogLevel.INFO, string logsDirectory = "./logs/", int maxOld = 5)
        {
            this.Name = name;
            this.Type = type;
            this.CachedThreshold = cacheThreshold;
            this.Level = defaultLevel;
            this.LogsDirectory = logsDirectory;
            this.LogFilePath = Path.Combine(Path.GetFullPath(this.LogsDirectory), $"{this.Name}.log");
            this.MaxOldFiles = maxOld;
            Directory.CreateDirectory(Path.GetDirectoryName(this.LogFilePath));
            this.CycleFiles();
            if (this.Type == LoggerType.CACHED)
            {
                AppDomain.CurrentDomain.ProcessExit += (s, e) => { this.WritePendingLines(); };
            }
        }

        /// <summary>
        /// Sets the <see cref="LogLevel"/> of this <see cref="Logger"/>.
        /// </summary>
        /// <param name="level">The <see cref="LogLevel"/> this <see cref="Logger"/> should be set to.</param>
        public void Setlevel(LogLevel level)
        {
            this.Level = level;
            this.Log($"Log level changed to '{this.Level}'.", (LogLevel)999);
        }
        /// <inheritdoc cref="Logger.Setlevel(LogLevel)"/>
        public void SetLevel(LogLevel level) => Setlevel(level);

        /// <inheritdoc cref="Logger.SetName(string, bool)"/>
        public void Rename(string name, bool renameFile = true) => SetName(name, renameFile);
        /// <summary>
        /// Changes the name of this <see cref="Logger"/> to the name specified.<br />
        /// If <paramref name="renameFile"/> is true, the log file on disk will also be renamed (if it exists).
        /// </summary>
        /// <param name="name">The new name of the logger</param>
        /// <param name="renameFile"></param>
        public void SetName(string name, bool renameFile = true)
        {
            this._renameInProgress = true;
            string oldName = this.Name;
            this.Name = name;
            string oldPath = this.LogFilePath;
            this.LogFilePath = Path.Combine(Path.GetFullPath(this.LogsDirectory), $"{this.Name}.log");
            if (renameFile && File.Exists(oldPath))
            {
                File.Move(oldPath, this.LogFilePath);
            }
            this._renameInProgress = false;
            this.Log($"Logger name was changed to '{this.Name}' (was '{oldName}').", (LogLevel)999);
        }
        /// <summary>
        /// Causes this <see cref="Logger"/> to push all of its pending lines (if any) to its respective log file.
        /// </summary>
        private void WritePendingLines()
        {
            if (this.OutputCache.Count > 0)
            {
                using (StreamWriter sr = new StreamWriter(this.LogFilePath, append: true, encoding: Encoding.UTF8))
                {
                    List<string> cache = this.OutputCache.ToList();
                    this.OutputCache.Clear();
                    sr.Write(string.Join("\n", cache) + "\n");
                    cache.Clear();
                }
            }
        }

        /// <summary>
        /// Sets which output methods are used for debug builds<br />
        /// <b>Note</b>: This function is only available when running under a debug context. Calls to this function when <b>not</b> under debug will do nothing.
        /// </summary>
        /// <param name="console">Whether output to <see cref="System.Console"/> is enabled.</param>
        /// <param name="debug">Whether output to <see cref="System.Diagnostics.Debug"/> is enabled.<br /><b>Note</b>: This setting is ignored if no debugger is detected.</param>
        [Conditional("DEBUG")]
        public void SupressDebugOutput(bool console = false, bool debug = false)
        {
            this.DebugOutputConsoleDisabled = console;
            this.DebugOutputDebugDisabled = debug;
        }

        /// <summary>
        /// Causes this <see cref="Logger"/> to cycle out its log files if <see cref="Logger.MaxOldFiles"/> is not 0.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this <see cref="Logger.MaxOldFiles"/> is 0, no log files are retained when cycling files. If the log file exists, it will be deleted.
        /// </para>
        /// <para>
        /// If this <see cref="Logger.MaxOldFiles"/> is 1, the last log file will be renamed to <c>&lt;name&gt;.log.last</c>
        /// </para>
        /// <para>
        /// If this <see cref="Logger.MaxOldFiles"/> is 2 or greater, log files will be cycled through incremently as <c>&lt;name&gt;.log.&lt;num&gt;</c> where <c>num</c> is an integer of increasing value up until the value of <see cref="Logger.MaxOldFiles"/>.
        /// <br />If a file already exists with <c>num</c> equal to <see cref="Logger.MaxOldFiles"/>, it is deleted.
        /// </para>
        /// </remarks>
        private void CycleFiles()
        {
            if (this.MaxOldFiles == 0)
            {
                if (File.Exists(this.LogFilePath))
                    File.Delete(this.LogFilePath);
            }
            else if (this.MaxOldFiles == 1)
            {
                if (File.Exists($"{this.LogFilePath}.last"))
                {
                    File.Delete($"{this.LogFilePath}.last");
                }
                if (File.Exists(this.LogFilePath))
                    File.Move(this.LogFilePath, $"{this.LogFilePath}.last");
            }
            else
            {
                for (int x = this.MaxOldFiles; x > 0; x--)
                {
                    string fileName = $"{this.LogFilePath}.{x}";
                    if (x == this.MaxOldFiles)
                    {
                        if (File.Exists(fileName))
                            File.Delete(fileName);
                        continue;
                    }

                    if (!File.Exists(fileName))
                        continue;
                    if (File.Exists(fileName))
                    {
                        string newPath = Path.ChangeExtension(fileName, (x + 1).ToString());
                        File.Move(fileName, newPath);
                    }

                }
                if (File.Exists(this.LogFilePath))
                {
                    File.Move(this.LogFilePath, $"{this.LogFilePath}.1");
                }
            }
        }

        /// <summary>
        /// Logs a message with a <see cref="LogLevel"/> of <see cref="LogLevel.DEBUG"/>.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0"/>.</param>
        public void Debug(string str, params object[] fillers) => this.Log(str, LogLevel.DEBUG, fillers);
        /// <summary>
        /// Logs a message with a <see cref="LogLevel"/> of <see cref="LogLevel.INFO"/>.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0"/>.</param>
        public void Info(string str, params object[] fillers) => this.Log(str, LogLevel.INFO, fillers);
        /// <summary>
        /// Logs a message with a <see cref="LogLevel"/> of <see cref="LogLevel.WARNING"/>.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0"/>.</param>
        public void Warning(string str, params object[] fillers) => this.Log(str, LogLevel.WARNING, fillers);
        /// <summary>
        /// Logs a message with a <see cref="LogLevel"/> of <see cref="LogLevel.ERROR"/>.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0"/>.</param>
        public void Error(string str, params object[] fillers) => this.Log(str, LogLevel.ERROR, fillers);
        /// <inheritdoc cref="Logger.Info(string, object[])"/>
        public void Log(string str, params object[] fillers) => this.Log(str, LogLevel.INFO, fillers);
        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="str">The <see cref="String"/> to log.</param>
        /// <param name="level">The <see cref="LogLevel"/> to log this message as.</param>
        /// <param name="fillers"><c>(optional)</c> The values you wish to use for string formatting. See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.string.format?view=net-6.0"/>.</param>
        private void Log(string str, LogLevel level, params object[] fillers)
        {

            if (level < this.Level) { return; }

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

            string time = DateTime.Now.ToString("yyyy-MM-dd HH\\:mm\\:ss");
            string line = string.Empty;

            line = string.Format("[{0}] [{1}] {2}: {3}", time, prefix.PadRight(7, ' '), this.Name, msg);
#if DEBUG
            if (!this.DebugOutputDebugDisabled && Debugger.IsAttached) { System.Diagnostics.Debug.WriteLine(line); }
            if (!this.DebugOutputConsoleDisabled) { Console.WriteLine(line); }
#endif
            this.AddLineToCache(line);

        }

        /// <summary>
        /// Adds a log line to the list of lines that need writing to file for this <see cref="Logger"/>.
        /// </summary>
        /// <param name="line"></param>
        private void AddLineToCache(string line)
        {
            this.OutputCache.Add(line);
            if (!this._renameInProgress && (this.Type == LoggerType.REALTIME || this.OutputCache.Count >= this.CachedThreshold))
            {
                this.WritePendingLines();
            }
        }

        /// <summary>
        /// Called when the class is being disposed. Tells the <see cref="Logger"/> to write all pending lines to file immediately.
        /// <br /><br /><b>Note</b>: There is no guarantee that finalisers are called in .NET6 or higher, so in this version, this class implements both a finaliser and <see cref="IDisposable.Dispose()"/> to give it the highest chance of pushing pending lines to file.
        /// </summary>
        public void Dispose()
        {
            if (!this.Disposed)
            {
                this.Disposed = true;
                GC.SuppressFinalize(this);
                this.WritePendingLines();
            }
        }

        /// <summary>
        /// Called when the class is being finalised. Tells the <see cref="Logger"/> to write all pending lines to file immediately.
        /// <br /><see href="https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/finalizers"/>
        /// <br /><br /><b>Note</b>: There is no guarantee that finalisers are called in .NET6 or higher, so in this version, this class implements both a finaliser and <see cref="IDisposable.Dispose()"/> to give it the highest chance of pushing pending lines to file.
        /// </summary>
        ~Logger()
        {
            this.WritePendingLines();
        }

    }
}
