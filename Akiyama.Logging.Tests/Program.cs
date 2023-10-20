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

            Console.ReadLine();

        }
    }
}
