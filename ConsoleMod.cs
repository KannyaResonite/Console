using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using ResoniteModLoader;

namespace Console
{
    public class ConsoleMod : ResoniteMod
    {
        public override string Name => "Console";
        public override string Author => "Kannya";
        public override string Version => "1.0.0";
        
        [AutoRegisterConfigKey]
        private static ModConfigurationKey<bool> LogToFile = new ModConfigurationKey<bool>("Log To File", "Whether console should forward the logged data into a txt file.", () => true);
        
        // allocconsole
        [DllImport("kernel32.dll")]
        public static extern bool AllocConsole();

        private static Type loggerType;

        private static ModConfiguration Config;

        private static DateTime StartDate;
        
        public override void OnEngineInit()
        {
            StartDate = DateTime.Now;
            
            Config = GetConfiguration();
            Config?.Save(true);
            
            if (AllocConsole())
            {
                var writer = new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true };
                System.Console.SetOut(writer);

                System.Console.Title = "Resonite | Kannya's Console";
                
                // reflection to get a internal sealed class, i hate it
                try
                {
                    var harmony = new Harmony("com.Kannya.Console");
                    
                    var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                    
                    loggerType = loadedAssemblies.FirstOrDefault(o => o?.GetType("ResoniteModLoader.Logger") != null)?.GetType("ResoniteModLoader.Logger") ?? loadedAssemblies.First(o => o.GetType("MonkeyLoader.Logging.LoggingController") != null).GetType("MonkeyLoader.Logging.LoggingController");

                    foreach (var logmethod in loggerType.GetMethods(AccessTools.all).Where(o => o.Name == "LogInternal"))
                    {
                        if (logmethod.GetParameters()[2].ParameterType == typeof(Func<object>))
                        {
                            harmony.Patch(logmethod, null, new HarmonyMethod(typeof(ConsoleMod).GetMethod(nameof(MonkeyLogToConsole), BindingFlags.Public | BindingFlags.Static)));
                        }
                        else if (logmethod.GetParameters()[2].ParameterType == typeof(IEnumerable<Func<object>>))
                        {
                            harmony.Patch(logmethod, null, new HarmonyMethod(typeof(ConsoleMod).GetMethod(nameof(MonkeyLogToConsoleMass), BindingFlags.Public | BindingFlags.Static)));
                        }
                        else
                        {
                            harmony.Patch(logmethod, null, new HarmonyMethod(typeof(ConsoleMod).GetMethod(nameof(LogInternal), BindingFlags.Public | BindingFlags.Static)));
                        }
                    }
                }
                catch (Exception e)
                {
                    Error("Failed to patch LogInternal! :(");
                    System.Console.WriteLine(e.ToString());
                    throw;
                }
                
                Msg("Hello World! - If you see this in the console, it's working correctly!");
            }
            else
            {
                Error("Failed to allocate console! :(");
            }
        }
        
        public enum LoggingLevel
        {
            Fatal = -3,
            Error = -2,
            Warn = -1,
            Info = 0,
            Debug = 1,
            Trace = 2
        }

        public static void MonkeyLogToConsoleMass(object __0, string __1, IEnumerable<Func<object>> __2)
        {
            foreach (var message in __2)
            {
                MonkeyLogToConsole(__0, __1, message);
            }
        }
        
        public static void MonkeyLogToConsole(object __0, string __1, Func<object> __2)
        {
            switch ((LoggingLevel)__0)
            {
                case LoggingLevel.Debug:
                case LoggingLevel.Trace:
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case LoggingLevel.Info:
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LoggingLevel.Warn:
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LoggingLevel.Error:
                case LoggingLevel.Fatal:
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            
            LogToConsole($"{Enum.GetName(typeof(LoggingLevel), (LoggingLevel)__0)} [{__1}] {__2()}");
        }

        public static void LogToConsole(string text)
        {
            text = $"[{DateTime.Now:dd/MM/yyyy hh:mm:ss tt}] {text}"; // UK date layout as i'm from scotland, deal with it :)
            
            System.Console.WriteLine(text);

            if (Config.GetValue(LogToFile))
            {
                Directory.CreateDirectory($"{Environment.CurrentDirectory}\\rml_logs");
                
                File.AppendAllText($"{Environment.CurrentDirectory}\\rml_logs\\RML_{StartDate:dd-MM-yyyy hh-mm-ss tt}.txt", text + "\r\n");
            }
        }

        #region Big chungus of methods
        
        public static void LogInternal(string __0, object __1, string __2)
        {
            switch (__0)
            {
                case "[DEBUG]":
                    System.Console.ForegroundColor = ConsoleColor.Cyan;
                    break;
                case "[INFO] ":
                    System.Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case "[WARN] ":
                    System.Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case "[ERROR]":
                    System.Console.ForegroundColor = ConsoleColor.Red;
                    break;
                default:
                    System.Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            
            if (__2 == null)
            {
                LogToConsole($"{__0}[ResoniteModLoader] {__1}");
                return;
            }
            
            LogToConsole($"{__0}[ResoniteModLoader/{__2}] {__1}");
        }
        
        #endregion
    }
}