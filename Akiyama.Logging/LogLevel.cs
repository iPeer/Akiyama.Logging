using System;

namespace Akiyama.Logging
{
    public enum LogLevel
    {
        DEBUG = 0,
        INFO = 1,
        WARNING = 2,
        WARN = 2,
        ERROR = 3,
        [Obsolete("Short-name variants of logging levels are deprecated, use LogLevel.DEBUG instead")]
        DBG = 0,
        [Obsolete("Short-name variants of logging levels are deprecated, use LogLevel.INFO instead")]
        INF = 1,
        [Obsolete("Short-name variants of logging levels are deprecated, use LogLevel.WARNING instead")]
        WRN = 2,
        [Obsolete("Short-name variants of logging levels are deprecated, use LogLevel.ERROR instead")]
        ERR = 3,
    }
}
