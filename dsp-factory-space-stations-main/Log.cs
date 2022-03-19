using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;

namespace DSPFactorySpaceStations
{
    public class Log
    {
        public static ManualLogSource logger;

        public static void Custom(LogLevel level, object data) => logger.Log(level, data);

        public static void Fatal(object data) => Custom(LogLevel.Fatal, data);

        public static void Error(object data) => Custom(LogLevel.Error, data);

        public static void Warning(object data) => Custom(LogLevel.Warning, data);

        public static void Message(object data) => Custom(LogLevel.Message, data);

        public static void Info(object data) => Custom(LogLevel.Info, data);

        public static void Debug(object data) => Custom(LogLevel.Debug, data);
    }
}
