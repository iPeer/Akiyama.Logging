using System;

namespace Akiyama.Logging.Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Logger l1 = new Logger("Hello", cacheThreshold: 3, maxOld: 2);
            Logger l2 = new Logger("World", type: LoggerType.REALTIME, maxOld: 1);
            Logger l3 = new Logger("Debug_Test", type: LoggerType.REALTIME, maxOld: 1);
            Logger l4 = new Logger("NonDefaultPath", type: LoggerType.REALTIME, maxOld: 1, logsDirectory: "./logs/wow/look/at/this");

            Logger l5 = new Logger("RenameTestFM", type: LoggerType.REALTIME, maxOld: 1);
            Logger l6 = new Logger("RenameTest", type: LoggerType.REALTIME, maxOld: 1);

            foreach (Logger logger in new Logger[] { l1, l2, l3, l4, l5, l6 }) 
            {
                logger.SupressDebugOutput(console: false, debug: false);
            }

            for (int x = 0; x < 5; x++)
            {
                l1.Info("Line {0}", x);
            }

            l2.Info("World Info");
            l2.Warning("World Warning");

            l3.Debug("Pre level set");
            l3.Setlevel(LogLevel.DEBUG);
            l3.Debug("After level set");

            l4.Info("Wow, it's amazing!");

            l5.Info("This logger");
            l5.Rename("RenameTestMoved");
            l5.Info("...was renamed!");

            l6.Info("This logger");
            l6.Rename("RenameTestNotMoved", renameFile: false);
            l6.Info("...was renamed, but not the file!");

            Console.ReadLine();

        }
    }
}
