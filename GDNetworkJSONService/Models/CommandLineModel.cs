using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using GDNetworkJSONService.Helpers;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON.ExtensionMethods;

namespace GDNetworkJSONService.Models
{
    internal class CommandLineModel
    {
        public const string AppBinaryName = "NetworkJsonGDService";
        
        private List<string> __helpInfo;
        public List<string> HelpInfo
        {
            get
            {
                if (__helpInfo == null)
                {
                    __helpInfo = new List<string>
                    {
                        @"This service acts as the guaranteed delivery service for NetworkJSON logging.",
                        @"It can be run as a windows service or at the command line. It runs on localhost",
                        @"and provides the 'store and forward' capability of the NLog.Targets.NetworkJSON",
                        @"NLog Target.",
                        @" ",
                        $"{AppBinaryName} /CONSOLE /GDDBSPATH='C:\\somewhere' [/DBSELECTCOUNT=X]",
                        @"     [/MULTIWRITEPAUSE=X] [/MTDL=X]",
                        @" ",
                        @"  /CONSOLE                  Run this app in console mode, if this is NOT SET then",
                        @"                            the application will attempt to start as a service.",
                        @"  /GDDBSPATH                The location where Guaranteed Delivery DBs will reside.",
                        @"  /DBSELECTCOUNT            The number of log messages to read from the Log Storage DB",
                        @"                            in a single SELECT statement.",
                        @"  /MULTIWRITEPAUSE          The number of milliseconds to wait before attempting to process",
                        @"                            more messages with a multiwrite target if the logging database",
                        @"                            does not return DBSELECTCOUNT messages.",
                        @"  /MTDL                     The number of minutes on SUBSEQUENT retries of attempting to",
                        @"                            send a log message before it is considered a 'Dead Letter'",
                        $"                            and is moved to the {DeadLetterLogStorageTable.TableName} table.",
                        @"  /?                        Display this help information.",
                        @" "
                    };
                }

                return (__helpInfo);
            }
        }

        private List<string> __errorInfo = new List<string>();
        public List<string> ErrorInfo
        {
            get { return (__errorInfo); }
        }

        private List<string> __parameterInfo = new List<string>();
        public List<string> ParameterInfo
        {
            get { return (__parameterInfo); }
        }

        public ParseCommandLineStatus LoadParameterInfo(string[] args)
        {
            var parameterStatus = ParseCommandLineStatus.ExecuteProgram;
            if (args.Length == 0 || args[0].IsSameCommandLineArg("?") || args[0].IsSameCommandLineArg("help"))
            {
                parameterStatus = ParseCommandLineStatus.DisplayHelp;
            }
            else
            {
                foreach (var arg in args)
                {
                    if (arg.IsSameCommandLineArg("console"))
                    {
                        ConsoleMode = true;
                    }
                    ProcessOptionalCommandLineEntry(arg);
                }
            }
            if (GdDbsPath.IsNullOrEmpty())
            {
                GdDbsPath = ConfigurationManager.AppSettings["GdDbsPath"];
                if (GdDbsPath.IsNullOrEmpty())
                {
                    ErrorInfo.Add("Guaranteed Delivery DBs Path is not properly setup in the application configuration and was not passed at the command line.");
                }
            }
            if (DbSelectCount < 1)
            {
                DbSelectCount = AppSettingsHelper.DbSelectCount;
            }
            if (MultiWritePause < 1)
            {
                MultiWritePause = AppSettingsHelper.MultiWritePause;
            }
            if (MinutesToDeadLetter < 1)
            {
                MinutesToDeadLetter = AppSettingsHelper.MinutesToDeadLetter;
            }
            
            if (ErrorInfo.Count > 0)
            {
                parameterStatus = ParseCommandLineStatus.DisplayError;
                ErrorInfo.Add("Use /? to display help information about this program.");
            }
            return parameterStatus;
        }

        private void ProcessOptionalCommandLineEntry(string commandLineEntry)
        {
            if (commandLineEntry.StartsWithCommandLineArg("dbselectcount"))
            {
                var arg = commandLineEntry.Split('=');
                var dbReadCount = -1;

                if ((arg.Length == 2) && int.TryParse(arg[1], out dbReadCount) && dbReadCount > 0)
                {
                    DbSelectCount = dbReadCount;
                }
                else
                {
                    ErrorInfo.Add("Invalid DBSELECTCOUNT parameter");
                }
            }
            else if (commandLineEntry.StartsWithCommandLineArg("multiwritepause"))
            {
                var arg = commandLineEntry.Split('=');
                var multiWritePause = -1;

                if ((arg.Length == 2) && int.TryParse(arg[1], out multiWritePause) && multiWritePause > 0)
                {
                    MultiWritePause = multiWritePause;
                }
                else
                {
                    ErrorInfo.Add("Invalid MULTIWRITEPAUSE parameter");
                }
            }
            else if (commandLineEntry.StartsWithCommandLineArg("mtdl"))
            {
                var arg = commandLineEntry.Split('=');
                var mtdl = -1;

                if ((arg.Length == 2) && int.TryParse(arg[1], out mtdl) && mtdl > 0)
                {
                    MinutesToDeadLetter = mtdl;
                }
                else
                {
                    ErrorInfo.Add("Invalid MTDL parameter");
                }
            }
            else if (commandLineEntry.StartsWithCommandLineArg("gddbspath"))
            {
                var arg = commandLineEntry.Split('=');
                GdDbsPath = arg[1];
            }
        }

        public void SetForServiceRun()
        {
            
        }

        #region Properties

        private bool _consoleMode;
        public bool ConsoleMode
        {
            get
            {
                return _consoleMode;
            }
            set
            {
                _consoleMode = value;
                ParameterInfo.Add($"Console Mode = {_consoleMode}");
            }
        }
        
        public string GdDbsPath
        {
            get
            {
                return LogStorageDbGlobals.GdDbsPath;
            }
            set
            {
                if (value.IsNullOrEmpty())
                {
                    LogStorageDbGlobals.GdDbsPath = "";
                    return;
                }
                
                try
                {
                    if (value.Length > 3 && (value.StartsWith("\"") || value.StartsWith("'")))
                    {
                        LogStorageDbGlobals.GdDbsPath = value.Substring(0, value.Length - 2);
                    }
                    else
                    {
                        LogStorageDbGlobals.GdDbsPath = value;
                    }

                    if (!Directory.Exists(value)) Directory.CreateDirectory(value);
                    ParameterInfo.Add($"Guaranteed Delivery DBs Path = {LogStorageDbGlobals.GdDbsPath}");
                }
                catch
                {
                    throw new Exception($"GdDbsPath '{value}' could not be created.");
                }
            }
        }

        public int DbSelectCount
        {
            get
            {
                return LogStorageDbGlobals.DbSelectCount;
            }
            set
            {
                if (value < 1)
                {
                    return;
                }
                LogStorageDbGlobals.DbSelectCount = value;
                ParameterInfo.Add($"DB Select Count = {LogStorageDbGlobals.DbSelectCount}");
            }
        }

        public int MultiWritePause
        {
            get
            {
                return LogStorageDbGlobals.MultiWritePause;
            }
            set
            {
                if (value < 1)
                {
                    return;
                }
                LogStorageDbGlobals.MultiWritePause = value;
                ParameterInfo.Add($"MultiWrite Pause (MS) = {LogStorageDbGlobals.MultiWritePause}");
            }
        }

        public int MinutesToDeadLetter
        {
            get
            {
                return LogStorageDbGlobals.MinutesTillDeadLetter;
            }
            set
            {
                if (value < 1)
                {
                    return;
                }
                LogStorageDbGlobals.MinutesTillDeadLetter = value;
                ParameterInfo.Add($"Minutes Till Dead Letter = {LogStorageDbGlobals.MinutesTillDeadLetter}");
            }
        }

        #endregion Properties

        public enum ParseCommandLineStatus
        {
            DisplayHelp,
            DisplayError,
            ExecuteProgram
        }
    }
}
