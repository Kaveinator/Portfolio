using WebServer.Commands;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace WebServer {
    public static class EventLog {
        static JSONFile Config = Properties.GetOrCreate(typeof(EventLog));
        static string DateTimeFormat => Config.GetValueOrDefault(nameof(DateTimeFormat), "M-dd-yyyy HH:mm:ss").Value;
        static int CaretStartPos => Config.GetValueOrDefault(nameof(CaretStartPos), 50).AsInt;
        static EventLevel MinimumEventLogLevel => Config.GetEnumOrDefault(nameof(MinimumEventLogLevel), EventLevel.Informational);

        static EventLog() {
            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
            };
            Console.TreatControlCAsInput = true;
        }

        static string Format(EventLevel level, string source, object msg) {
            string levelText = level switch {
                EventLevel.Verbose => "Debug",
                EventLevel.Informational => "Info",
                EventLevel.LogAlways => "Info",
                EventLevel.Warning => "Warn",
                EventLevel.Error => "Error",
                EventLevel.Critical => "Fatal",
                _ => "null"
            };
            //if (string.IsNullOrEmpty(source)) source = "<unknown>";
            string additionalSpaceAfterLevel = new string(' ', Math.Max(5 - levelText.ToString().Length, 0));
            string output = $"[{DateTime.Now.ToString(DateTimeFormat)}] [{levelText}]{additionalSpaceAfterLevel} {(string.IsNullOrEmpty(source) ? string.Empty : $"[{source}]")}";
            output += $"{new string(' ', Math.Max(0, CaretStartPos - output.Length))}: {msg}";
            return output;
        }

        static object ThreadLock = new object();
        public static void LogDebug(object msg, string source, bool? forcePrint) {
            lock (ThreadLock) {
                if (forcePrint.HasValue ? !forcePrint.Value : EventLevel.Verbose > MinimumEventLogLevel) return;
                string log = Format(EventLevel.Verbose, source, msg);
                ConsoleColor PrevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                ClearCurrentConsoleLine();
                Console.WriteLine(log);
                Console.ForegroundColor = PrevColor;
                RedrawInput();
            }
        }

        public static void Log(object msg, string source, bool? forcePrint) {
            if (forcePrint.HasValue ? !forcePrint.Value : EventLevel.Informational > MinimumEventLogLevel) return;
            lock (ThreadLock) {
                string log = Format(EventLevel.Informational, source, msg);
                ConsoleColor PrevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                ClearCurrentConsoleLine();
                Console.WriteLine(log);
                Console.ForegroundColor = PrevColor;
                RedrawInput();
            }
        }

        public static void LogWarn(object msg, string source, bool? forcePrint) {
            if (forcePrint.HasValue ? !forcePrint.Value : EventLevel.Warning > MinimumEventLogLevel) return;
            lock (ThreadLock) {
                string log = Format(EventLevel.Warning, source, msg);
                ConsoleColor PrevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Yellow;
                ClearCurrentConsoleLine();
                Console.WriteLine(log);
                Console.ForegroundColor = PrevColor;
                RedrawInput();
            }
        }

        public static void LogError(object msg, string source, bool? forcePrint) {
            if (forcePrint.HasValue ? !forcePrint.Value : EventLevel.Error > MinimumEventLogLevel) return;
            lock (ThreadLock) {
                string log = Format(EventLevel.Error, source, msg);
                ConsoleColor PrevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                ClearCurrentConsoleLine();
                Console.WriteLine(log);
                Console.ForegroundColor = PrevColor;
                RedrawInput();
            }
        }

        public static void LogExplicit(object msg, string source) {
            lock (ThreadLock) {
                string log = Format(EventLevel.LogAlways, source, msg);
                ConsoleColor PrevColor = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Cyan;
                ClearCurrentConsoleLine();
                Console.WriteLine(log);
                Console.ForegroundColor = PrevColor;
                RedrawInput();
            }
        }
        static bool IsInputActive = false;
        static StringBuilder ConsoleLabel = new StringBuilder();
        static StringBuilder ConsoleInput = new StringBuilder();
        /// <summary>Reads input from user</summary>
        /// <param name="label">Whats the input your trying to get?</param>
        /// <param name="source">Where did the request come from? (or what is it for)</param>
        /// <param name="isInputSensitive">Should the input be hidden from plain sight?</param>
        /// <returns></returns>
        /// <see href="https://stackoverflow.com/a/49511467">Implementation Source</see>
        public static string ReadLine(string label, string source, string placeholder, bool isInputSensitive) {
            IsInputActive = true;
            if (string.IsNullOrEmpty(label)) label = string.Empty;
            //if (string.IsNullOrEmpty(source)) source = $"[{Environment.MachineName}] ";
            //else source = $"[{Environment.MachineName}/{source}] ";
            ConsoleLabel.Clear();
            if (!string.IsNullOrEmpty(source)) ConsoleLabel.AppendFormat("/{0}", source);
            if (!string.IsNullOrEmpty(label)) ConsoleLabel.AppendFormat(":{0}", label);
            //ConsoleLabel = $"{source}: {label}";
            StringBuilder input = string.IsNullOrEmpty(placeholder) ? new StringBuilder() : new StringBuilder(placeholder);
            ConsoleInput = new StringBuilder(isInputSensitive ? new string('*', placeholder.Length) : placeholder);
            RedrawInput();
            Task.Run(async () => {
                try {
                    ConsoleKeyInfo keyInfo;
                    var startingLeft = Console.CursorLeft;
                    var startingTop = Console.CursorTop;
                    var currentIndex = input.Length;
                    do {
                        var previousLeft = Console.CursorLeft;
                        var previousTop = Console.CursorTop;
                        /*while (!Console.KeyAvailable) {
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.Delay(50);
                        }*/
                        keyInfo = Console.ReadKey(intercept: true);
                        if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Control) && keyInfo.Key == ConsoleKey.C)
                            throw new OperationCanceledException();
                        switch (keyInfo.Key) {
                            case ConsoleKey.A:
                            case ConsoleKey.B:
                            case ConsoleKey.C:
                            case ConsoleKey.D:
                            case ConsoleKey.E:
                            case ConsoleKey.F:
                            case ConsoleKey.G:
                            case ConsoleKey.H:
                            case ConsoleKey.I:
                            case ConsoleKey.J:
                            case ConsoleKey.K:
                            case ConsoleKey.L:
                            case ConsoleKey.M:
                            case ConsoleKey.N:
                            case ConsoleKey.O:
                            case ConsoleKey.P:
                            case ConsoleKey.Q:
                            case ConsoleKey.R:
                            case ConsoleKey.S:
                            case ConsoleKey.T:
                            case ConsoleKey.U:
                            case ConsoleKey.V:
                            case ConsoleKey.W:
                            case ConsoleKey.X:
                            case ConsoleKey.Y:
                            case ConsoleKey.Z:
                            case ConsoleKey.Spacebar:
                            case ConsoleKey.Decimal:
                            case ConsoleKey.Add:
                            case ConsoleKey.Subtract:
                            case ConsoleKey.Multiply:
                            case ConsoleKey.Divide:
                            case ConsoleKey.D0:
                            case ConsoleKey.D1:
                            case ConsoleKey.D2:
                            case ConsoleKey.D3:
                            case ConsoleKey.D4:
                            case ConsoleKey.D5:
                            case ConsoleKey.D6:
                            case ConsoleKey.D7:
                            case ConsoleKey.D8:
                            case ConsoleKey.D9:
                            case ConsoleKey.NumPad0:
                            case ConsoleKey.NumPad1:
                            case ConsoleKey.NumPad2:
                            case ConsoleKey.NumPad3:
                            case ConsoleKey.NumPad4:
                            case ConsoleKey.NumPad5:
                            case ConsoleKey.NumPad6:
                            case ConsoleKey.NumPad7:
                            case ConsoleKey.NumPad8:
                            case ConsoleKey.NumPad9:
                            case ConsoleKey.Oem1:
                            case ConsoleKey.Oem102:
                            case ConsoleKey.Oem2:
                            case ConsoleKey.Oem3:
                            case ConsoleKey.Oem4:
                            case ConsoleKey.Oem5:
                            case ConsoleKey.Oem6:
                            case ConsoleKey.Oem7:
                            case ConsoleKey.Oem8:
                            case ConsoleKey.OemComma:
                            case ConsoleKey.OemMinus:
                            case ConsoleKey.OemPeriod:
                            case ConsoleKey.OemPlus:
                                input.Insert(currentIndex, keyInfo.KeyChar);
                                ConsoleInput.Insert(currentIndex, isInputSensitive ? '*' : keyInfo.KeyChar);
                                currentIndex++;
                                /*if (currentIndex < input.Length) {
                                    var left = Console.CursorLeft;
                                    var top = Console.CursorTop;
                                    Console.Write(input.ToString().Substring(currentIndex));
                                    Console.SetCursorPosition(left, top);
                                }*/
                                RedrawInput();
                                break;
                            case ConsoleKey.Backspace:
                                if (currentIndex > 0) {
                                    currentIndex--;
                                    input.Remove(currentIndex, 1);
                                    ConsoleInput.Remove(currentIndex, 1);
                                }
                                /*var left = Console.CursorLeft;
                                var top = Console.CursorTop;
                                if (left == previousLeft) {
                                    left = Console.BufferWidth - 1;
                                    top--;
                                    Console.SetCursorPosition(left, top);
                                }
                                Console.Write(input.ToString().Substring(currentIndex) + " ");
                                Console.SetCursorPosition(left, top);
                            }
                            else {
                                Console.SetCursorPosition(startingLeft, startingTop);
                            }*/
                                RedrawInput();
                                break;
                            case ConsoleKey.Delete:
                                if (input.Length > currentIndex) {
                                    input.Remove(currentIndex, 1);
                                    ConsoleInput.Remove(currentIndex, 1);
                                    /*Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(input.ToString().Substring(currentIndex) + " ");
                                    Console.SetCursorPosition(previousLeft, previousTop);*/
                                }
                                //else Console.SetCursorPosition(previousLeft, previousTop);
                                RedrawInput();
                                break;
                            case ConsoleKey.LeftArrow:
                                if (currentIndex > 0) //{
                                    currentIndex--;/*
                                    var left = Console.CursorLeft - 1;
                                    var top = Console.CursorTop;
                                    if (left < 0) {
                                        left = Console.BufferWidth + left;
                                        top--;
                                    }
                                    Console.SetCursorPosition(left, top);
                                    if (currentIndex < input.Length - 1) {
                                        Console.Write(input[currentIndex].ToString() + input[currentIndex + 1]);
                                        Console.SetCursorPosition(left, top);
                                    }
                                }
                                else {
                                    Console.SetCursorPosition(startingLeft, startingTop);
                                    if (input.Length > 0)
                                        Console.Write(input[0]);
                                    Console.SetCursorPosition(startingLeft, startingTop);
                                }*/
                                break;
                            case ConsoleKey.RightArrow:
                                if (currentIndex < input.Length) //{
                                    currentIndex++;
                                /*Console.SetCursorPosition(previousLeft, previousTop);
                                Console.Write(input[currentIndex]);
                            }
                            else {
                                Console.SetCursorPosition(previousLeft, previousTop);
                            }*/
                                break;
                            case ConsoleKey.Home:
                                /*if (input.Length > 0 && currentIndex != input.Length) {
                                    Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(input[currentIndex]);
                                }
                                Console.SetCursorPosition(startingLeft, startingTop);*/
                                currentIndex = 0;
                                break;
                            case ConsoleKey.End:
                                if (currentIndex < input.Length) {
                                    /*Console.SetCursorPosition(previousLeft, previousTop);
                                    Console.Write(input[currentIndex]);
                                    var left = previousLeft + input.Length - currentIndex;
                                    var top = previousTop;
                                    while (left > Console.BufferWidth) {
                                        left -= Console.BufferWidth;
                                        top++;
                                    }*/
                                    currentIndex = input.Length;
                                    //Console.SetCursorPosition(left, top);
                                }
                                //else Console.SetCursorPosition(previousLeft, previousTop);
                                break;
                            default:
                                //Console.SetCursorPosition(previousLeft, previousTop);
                                break;
                        }
                    } while (keyInfo.Key != ConsoleKey.Enter);
                    Console.WriteLine();
                }
                catch (OperationCanceledException) {
                    input = null;
                }
                catch (Exception err) {
                    LogError(err, "EventLog", true);
                    //MARK: Change this based on your need. See description below.
                    //input.Clear();
                }
                ConsoleInput.Clear();
            }).Wait();
            IsInputActive = false;
            return input?.ToString();
        }

        static void RedrawInput() {
            ClearCurrentConsoleLine();
            if (!IsInputActive) return;
            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Write($"{Environment.UserName}@{Environment.UserDomainName}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"~{ConsoleLabel}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write($"$ {ConsoleInput}");
            Console.ForegroundColor = prevColor;
            //Console.Write(ConsoleLabel + ConsoleInput);
        }

        public static void ClearCurrentConsoleLine() {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }

        #region Commands
        static EventLogger Logger = EventLogger.GetOrCreate(typeof(EventLog));
        static List<ICommand> Commands { get; } = new List<ICommand>();

        public static bool HasCommand<T>() where T : ICommand => Commands.Any(o => o is T);

        public static void AddCommand<T>() where T : ICommand, new() {
            if (HasCommand<T>())
                return;
            Commands.Add(new T());
        }

        public static void RemoveCommand<T>() where T : ICommand {
            if (!HasCommand<T>())
                return;
            Commands.Remove(Commands.First(o => o is T));
        }

        public static void LaunchCommand<T>(string args = "") where T : ICommand, new() => LaunchCommand(new T(), args);

        public static void LaunchCommand(ICommand t, string args = "") => LaunchCommand(t, args.ParseCommandLineArguments());

        static void LaunchCommand(ICommand t, string[] args) {
            if (t == null) return;
            if (args == null) args = t.Aliases.Length > 0 ? new string[] { t.Aliases[0] } : Array.Empty<string>();
            //Logger.LogDebug($"Running command '{t.GetType().Name}'");
            t.Execute(args);
        }


        /// <returns>If a command was ran</returns>
        public static bool LaunchCommandfromInput(string rawInput) {
            if (string.IsNullOrEmpty(rawInput)) return false;

            string[] input = rawInput.ParseCommandLineArguments();
            // Commented out because might never be true, (because of null/empty check)
            // if (input.Length != 0) {
            List<ICommand> commands = Commands.FindAll(cmd => cmd.Aliases.Contains(input[0].ToLower()));
            string cmdName = commands.Count > 0 ? commands[0].GetType().Name : string.Empty;
            if (input[0] == "?") {
                if (Commands.Count == 0)
                    Logger.LogExplicit("Command list is empty!");
                else {
                    string msg = "Available Commands:";
                    Commands.ForEach(cmd => msg += $"\n  {string.Join("/", cmd.Aliases)} - {cmd.Name}: {cmd.Description}");
                    msg += "\n\nUse 'cmd ?' to see help for that command";
                    Logger.LogExplicit(msg);
                }
            }
            else if (commands.Count == 0)
                Logger.LogExplicit($"No command found! Use '?' for a list of commands");
            else if (commands.Count > 1) {
                StringBuilder error = new StringBuilder().AppendFormat("Multiple commands found with alias '{0}'", input[0]);
                commands.ForEach(cmd => error.AppendFormat("\n  {0} - {1}: {2}", string.Join("/", cmd.Aliases), cmd.Name, cmd.Description));
                Logger.LogExplicit(error);
            }
            else if (input.Length > 1 && input[input.Length - 1] == "?")
                Logger.LogExplicit(commands[0].Help(input));
            else {
                try { LaunchCommand(commands[0], input); }
                catch (OperationCanceledException) { Logger.LogWarning($"{cmdName} Execution Canceled", true); }
                catch (Exception err) { Logger.LogError($"{cmdName} Error: {err}", true); }
            }
            return true;
        }

        /* Older implementation:
        public virtual bool HasCommand(ICommand command) => Commands.Any(o => o.GetType() == command.GetType());

        public virtual void AddCommand(ICommand command) {
            if (HasCommand(command))
                return;
            //EventLogger.Log($"New Command '{command.GetType().Name}'");
            Commands.Add(command);
        }

        public virtual void RemoveCommand(ICommand command) {
            if (!HasCommand(command))
                return;
            Type commandType = command.GetType();
            //EventLogger.Log($"Command '{commandType.Name}' removed");
            Commands.Remove(Commands.First(o => o == command));
        }*/
        #endregion

    }
    public struct EventLogger {
        public static Dictionary<string, EventLogger> Instances = new Dictionary<string, EventLogger>();
        public bool? ForcePrint;
        public static EventLogger GetOrCreate<T>(bool? forcePrint = null) => GetOrCreate(typeof(T), forcePrint);
        public static EventLogger GetOrCreate(Type t, bool? forcePrint = null) => GetOrCreate(t.Name, forcePrint);
        public static EventLogger GetOrCreate(string source, bool? forcePrint = null) {
            if (!Instances.TryGetValue(source, out var logger))
                Instances.Add(source, logger = new EventLogger(source, forcePrint));
            else if (forcePrint.HasValue)
                logger.ForcePrint = forcePrint.Value;
            return logger;
        }

        public readonly string Source;
        EventLogger(string source, bool? forcePrint = null) {
            Source = source;
            ForcePrint = forcePrint;
        }

        public void LogDebug(object msg, bool? forcePrint = null) => EventLog.LogDebug(msg, Source, forcePrint.HasValue ? forcePrint.Value : forcePrint);
        public void Log(object msg, bool? forcePrint = null) => EventLog.Log(msg, Source, forcePrint.HasValue ? forcePrint.Value : forcePrint);
        public void LogWarning(object msg, bool? forcePrint = null) => EventLog.LogWarn(msg, Source, forcePrint.HasValue ? forcePrint.Value : forcePrint);
        public void LogError(object msg, bool? forcePrint = null) => EventLog.LogError(msg, Source, forcePrint.HasValue ? forcePrint.Value : forcePrint);
        public void LogExplicit(object msg) => EventLog.LogExplicit(msg, Source);

        public string ReadLine(string label = "", string placeholder = "", bool isInputSensitive = false) => EventLog.ReadLine(label, Source, placeholder, isInputSensitive);
    }
}
