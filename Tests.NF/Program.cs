using Akiyama.Logging.Configuration;
using Akiyama.Logging.Levels;
using Akiyama.Logging.Loggers;
using Akiyama.Logging.Types;

namespace Tests.NF
{
    internal class Program
    {
        static void Main(string[] args)
        {
            
            LoggerConfig @default = new LoggerConfig("DefaultOutput", LoggerType.REALTIME, LogLevel.DEBUG, formatter: LogFormatter.DEFAULT);
            LoggerConfig emoji = new LoggerConfig("EmojiOutput", LoggerType.REALTIME, LogLevel.DEBUG, formatter: LogFormatter.EMOJI);

            FileLogger fl_default = new FileLogger(@default);
            FileLogger fl_emoji = new FileLogger(emoji);

            fl_default.SetLevel(LogLevel.DEBUG);
            fl_emoji.SetLevel(LogLevel.DEBUG);

            fl_default.Debug("Test at DEBUG level");
            fl_default.Info("Test at INFO level");
            fl_default.Warning("Test at WARNING level");
            fl_default.Error("Test at ERROR level");


            fl_emoji.Debug("Test at DEBUG level");
            fl_emoji.Info("Test at INFO level");
            fl_emoji.Warning("Test at WARNING level");
            fl_emoji.Error("Test at ERROR level");


        }
    }
}
