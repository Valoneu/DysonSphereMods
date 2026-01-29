using BepInEx.Logging;
using System;
using System.Linq;
using UnityEngine;

namespace DysonSphereMods.Shared
{
    public static class Log
    {
        private static ManualLogSource _logger;

        public static void Init(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void Debug(object data) => _logger?.LogDebug(data);
        public static void Info(object data) => _logger?.LogInfo(data);
        public static void Warning(object data) => _logger?.LogWarning(data);
        public static void Error(object data) => _logger?.LogError(data);
        public static void Fatal(object data) => _logger?.LogFatal(data);
        public static void Message(object data) => _logger?.LogMessage(data);

        public static void LogOnce(string msg, ref bool flag, params object[] args)
        {
            if (flag)
                return;
            flag = true;
            try
            {
                 var argVals = args == null ? Array.Empty<string>() : args.Select(arg => arg == null ? "null" : (arg is int || arg is string || arg.GetType().IsPrimitive ? arg.ToString() : JsonUtility.ToJson(arg))).ToArray();
                 Info(string.Format(msg, argVals));
            }
            catch (Exception ex)
            {
                Warning($"LogOnce failed to format message: {msg}. Exception: {ex}");
            }
        }
    }
}