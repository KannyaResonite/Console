using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
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

        private static CultureInfo BothFormat;
        
        public override void OnEngineInit()
        {
            StartDate = DateTime.Now;
            BothFormat = new CultureInfo(Thread.CurrentThread.CurrentCulture.ToString());
            
            Config = GetConfiguration();
            Config?.Save(true);
            
            if (AllocConsole())
            {
                var writer = new StreamWriter(System.Console.OpenStandardOutput()) { AutoFlush = true };
                System.Console.SetOut(writer);

                System.Console.Title = "Resonite | Kannya's Console";
                
                var harmony = new Harmony("com.Kannya.Console");
            
                // reflection to get a internal sealed class, i hate it
                loggerType = typeof(ResoniteMod).Assembly.GetType("ResoniteModLoader.Logger");
            
                harmony.Patch(loggerType.GetMethod("LogInternal", AccessTools.all), null, new HarmonyMethod(typeof(ConsoleMod).GetMethod(nameof(LogInternal), BindingFlags.Public | BindingFlags.Static)));
            
                Msg("Hello World!");
            }
        }

        public static void LogToConsole(string text)
        {
            var FormattedDate = DateTime.Now.ToString(BothFormat.DateTimeFormat.ShortDatePattern, BothFormat);
            var FormattedTime = DateTime.Now.ToString(BothFormat.DateTimeFormat.ShortTimePattern, BothFormat);
            
            text = $"[{FormattedDate} ${FormattedTime}] {text}"; // UK date layout as i'm from scotland, deal with it :) // what if I dont wanna ?
            
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