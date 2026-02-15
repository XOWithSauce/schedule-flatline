
using HarmonyLib;
using MelonLoader;
using System.Runtime.CompilerServices;
using System.Collections;
using UnityEngine;

using static Flatline.ConsoleModule;
using static Flatline.DebugModule;
using static Flatline.Flatline;
using static Flatline.FlatlinePlayer;
using static Flatline.ConfigLoader;


#if MONO
using ConsoleType = ScheduleOne.Console;
#else
using ConsoleType = Il2CppScheduleOne.Console;
#endif

namespace Flatline
{
    public static class DebugModule
    {
        public static void Log(string msg, [CallerMemberName] string memberName = "")
        {
#if DEBUG
            // Debug builds log everything
            MelonLogger.Msg($"[{memberName}] {msg}");
#else
            // Player has to manually enable full logging otherwise its just console feedback
            if (isLoggingEnabled || ConsoleMethodNames.Contains(memberName))
                MelonLogger.Msg($"[{memberName}] {msg}");
#endif
        }

        #region Debug controls for console

        public static Dictionary<string, ConsoleCommandBase> consoleTargets = new()
        {
            { "player", new PlayerTarget() },
            { "disease", new DiseaseTarget() },
            { "consumption", new ConsumptionTarget() }
        };

        public static void RunCommand(List<string> args)
        {
            if (args.Count == 2)
            {
                if (args[1].ToLower() == "help")
                    Help();
                else if (args[1].ToLower() == "start")
                    Flatline.haltExecution = false;
                else if (args[1].ToLower() == "stop")
                    Flatline.haltExecution = true;
                else
                    Log("Usage: flatline (action) (method)\n    Try: flatline help");
                return;
            }

            if (args.Count == 3 && args[1].ToLower() == "enable" && args[2].ToLower() == "logs")
            {
                isLoggingEnabled = true;
                return;
            }

            if (args.Count < 3)
            {
                Log("Usage: flatline (action) (method)\n    Try: flatline help");
                return;
            }

            string actionStr = args[1].ToLower();
            string targetStr = args[2].ToLower();

            if (!consoleTargets.TryGetValue(targetStr, out ConsoleCommandBase target))
            {
                Log($"Unknown command target '{targetStr}'");
                return;
            }

            CommandSupport requestedMethod = actionStr switch
            {
                "set" => CommandSupport.Set,
                "list" => CommandSupport.List,
                "add" => CommandSupport.Add,
                _ => CommandSupport.None
            };

            if ((target.SupportedMethods & requestedMethod) == 0)
            {
                Log($"Command target '{targetStr}' does not support requested method '{requestedMethod}'");
                return;
            }

            switch (requestedMethod)
            {
                case CommandSupport.Set:
                    target.Execute(args);
                    break;
                case CommandSupport.List:
                    target.List();
                    break;
                case CommandSupport.Add:
                    target.Add(args);
                    break;
            }
        }
        public static void Help()
        {
            string listmessage = "";
            listmessage += "\nSupported Commands:";
            listmessage += $"\n\n# ENABLE FULL LOGGING";
            listmessage += $"\nflatline enable logs";

            listmessage += $"\n\n# START OR STOP UPDATING THE MOD EVENTS";
            listmessage += $"\nflatline start";
            listmessage += $"\nflatline stop";

            foreach (ConsoleCommandBase target in consoleTargets.Values)
            {
                listmessage += $"\n\n# {target.Name.ToUpper()}";
                if (target.SupportedMethods.HasFlag(CommandSupport.Set))
                {
                    if (target.Name.ToLower().Contains("disease"))
                        listmessage += $"\nflatline set {target.Name} (disease ID) (member) (member value)";
                    else
                        listmessage += $"\nflatline set {target.Name} (member) (member value)";
                }

                if (target.SupportedMethods.HasFlag(CommandSupport.List))
                    listmessage += $"\nflatline list {target.Name}";

                if (target.SupportedMethods.HasFlag(CommandSupport.Add))
                {
                    if (target.Name.ToLower().Contains("disease"))
                        listmessage += $"\nflatline add {target.Name} (disease ID)";
                }
            }
            Log(listmessage);
            return;
        }

        #endregion
    }

    // Patch the Console Submit command functions to add the Debug commands
#if MONO
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(List<string>) })]
#else
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
#endif
    public static class Console_SubmitCommand_ListString_Patch
    {
#if MONO
        public static bool Prefix(ConsoleType __instance, List<string> args)
        {
#else
        public static bool Prefix(ConsoleType __instance, Il2CppSystem.Collections.Generic.List<string> args)
        {
            List<string> managedArgs = new();
            foreach (string arg in args) // convert from il2cpp list object to normal
                managedArgs.Add(arg);
#endif

            if (args.Count == 0) return true;
            if (args[0].ToLower() == "flatline")
            {
#if MONO
                RunCommand(args);
#else
                RunCommand(managedArgs);
#endif
                return true;
            }
            return true;

        }
    }


    // This because it needs to be patched for the above patch to work
    [HarmonyPatch(typeof(ConsoleType), "SubmitCommand", new Type[] { typeof(string) })]
    public static class Console_SubmitCommand_String_Patch
    {
        public static bool Prefix(ConsoleType __instance, string args)
        {
            return true;
        }
    }
}