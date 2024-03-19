using Akiyama.Logging.Configuration;
using Akiyama.Logging.Levels;
using Akiyama.Logging.Loggers;
using Akiyama.Logging.Types;
using System;

namespace Tests.NF
{
    internal class Program
    {
        static void Main(string[] args)
        {

            LoggerConfig @default = new LoggerConfig("DefaultOutput", type: LoggerType.REALTIME, level: LogLevel.DEBUG, formatter: LogFormatter.DEFAULT);
            LoggerConfig emoji = new LoggerConfig("EmojiOutput", type: LoggerType.REALTIME, level: LogLevel.DEBUG, formatter: LogFormatter.EMOJI, maxCycledFiles: 3);
            LoggerConfig colour = new LoggerConfig("ColourOutput", type: LoggerType.REALTIME, level: LogLevel.DEBUG, formatter: LogFormatter.COLOUR, maxCycledFiles: 1);

            Logger fl_default = new Logger(@default);
            Logger fl_emoji = new Logger(emoji);
            Logger fl_colour = new Logger(colour);

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

            fl_colour.Debug("Test at DEBUG level");
            fl_colour.Info("Test at INFO level");
            fl_colour.Warning("Test at WARNING level");
            fl_colour.Error("Test at ERROR level");

            Console.WriteLine("Tests complete. Press any key to exit.");
            _ = Console.ReadKey();


        }
    }
}
