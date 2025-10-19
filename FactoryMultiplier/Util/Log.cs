using BepInEx.Logging;
using System;
using System.Linq;
using UnityEngine;

namespace FactoryMultiplier.Util
{
    public static class Log
    {
        public static ManualLogSource logger;


        public static void Debug(string message)
        {
            logger.LogDebug($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void Info(string message)
        {
            logger.LogInfo($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void Warn(string message)
        {
            logger.LogWarning($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        }

        public static void LogOnce(string msg, ref bool flag, params object[] args)
        {
            if (flag)
                return;
            flag = true;
            var argVals = args == null ? Array.Empty<string>() : args.ToList().Select(arg => arg is int ? arg.ToString() : JsonUtility.ToJson(arg)).ToArray();
            Info(string.Format(msg, argVals));
        }
    }
}