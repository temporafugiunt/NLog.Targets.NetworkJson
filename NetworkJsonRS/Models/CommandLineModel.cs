using System.Collections.Generic;
using NLog.Targets.NetworkJSON.Extensions;

namespace NetworkJsonRS.Models
{
    internal class CommandLineModel
    {
        public const string AppBinaryName = "NetworkJsonRS";
        
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
                        $"{AppBinaryName} /CONSOLE",
                        @" ",
                        @"  /CONSOLE                  Run this app in console mode, if this is NOT SET then",
                        @"                            the application will attempt to start as a service.",
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
            if (args.Length == 0)
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
                        ParameterInfo.Add("CONSOLEMODE = true");
                    }
                    //ProcessOptionalCommandLineEntry(arg);
                }

                if (ErrorInfo.Count > 0)
                {
                    parameterStatus = ParseCommandLineStatus.DisplayError;
                }
            }
            return parameterStatus;
        }

        //private void ProcessOptionalCommandLineEntry(string commandLineEntry)
        //{
            
        //}

        public void SetForServiceRun()
        {
            
        }

        #region Properties
        public bool ConsoleMode { get; set; }

        #endregion Properties

        public enum ParseCommandLineStatus
        {
            DisplayHelp,
            DisplayError,
            ExecuteProgram
        }
    }
}
