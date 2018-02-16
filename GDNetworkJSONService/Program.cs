using System;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using GDNetworkJSONService.Loggers;
using GDNetworkJSONService.Models;

namespace GDNetworkJSONService
{
    class Program
    {
        private static MessageLogger Logger { get; } = LoggerFactory.GetMessageLogger();

        static void Main(string[] args)
        {
            var model = new CommandLineModel();
            var status = model.LoadParameterInfo(args);
            try
            {
                if (model.ConsoleMode)
                {
                    var appTitle = $"{CommandLineModel.AppBinaryName} Service";
                    Console.Title = appTitle;
                    Console.WriteLine(appTitle);
                    Console.WriteLine(new string('=', appTitle.Length));

                    OutputModelInfo(status, model);

                    if (status != CommandLineModel.ParseCommandLineStatus.ExecuteProgram)
                    {
                        Environment.ExitCode = 1;
                        Thread.Sleep(500); // Allow NLog Console Logging.
                        return;
                    }
                    var reliabilityService = new GDService(model);
                    reliabilityService.OnStartConsoleMode();

                    Console.WriteLine("Hit any key to stop");
                    Console.ReadKey();
                    Console.WriteLine("Key Registered, Stopping ....");

                    reliabilityService.OnStopConsoleMode();
                    Thread.Sleep(500); // Allow NLog Console Logging.
                }
                else
                {
                    if (status == CommandLineModel.ParseCommandLineStatus.DisplayError)
                    {
                        var sb = new StringBuilder();
                        foreach (var error in model.ErrorInfo)
                        {
                            sb.AppendLine(error);
                        }
                        throw new Exception(sb.ToString());
                    }
                    // Set all defaults when running as a service if a command line wasn't set when installing the service.
                    model.SetForServiceRun();

                    OutputModelInfo(CommandLineModel.ParseCommandLineStatus.ExecuteProgram, model);

                    var service = new ServiceBase[]
                    {
                        new GDService(model),
                    };
                    ServiceBase.Run(service);
                }
            }
            catch (Exception ex)
            {
                Logger.LogFatal($"{CommandLineModel.AppBinaryName} Startup Exception", ex);
                Thread.Sleep(500); // Allow NLog Console Logging.
            }

        }


        private static void OutputModelInfo(CommandLineModel.ParseCommandLineStatus status, CommandLineModel model)
        {
            // Always display errors.
            if (status == CommandLineModel.ParseCommandLineStatus.DisplayError && model.ErrorInfo.Count > 0)
            {
                foreach (var error in model.ErrorInfo)
                {
                    Logger.PushError(error);
                }
                Logger.LogError($"{CommandLineModel.AppBinaryName} Command Line Errors:");
            }
            // Display help if asked.
            else if (status == CommandLineModel.ParseCommandLineStatus.DisplayHelp)
            {
                foreach (var info in model.HelpInfo)
                {
                    Logger.PushInfo(info);
                }
                Logger.LogInfo($"{CommandLineModel.AppBinaryName} Help:");
            }
            // Otherwise display the parameters that will be used for this execution run.
            else
            {
                if (model.ParameterInfo.Count > 0)
                {
                    Logger.PushHeaderInfo($"{CommandLineModel.AppBinaryName} Execution Parameters");
                    foreach (var info in model.ParameterInfo)
                    {
                        Logger.PushInfo(info);
                    }
                }
                if (model.ConsoleMode)
                {
                    Logger.LogInfo($"{CommandLineModel.AppBinaryName} Started on {DateTime.Now} in Console Mode.");
                }
                else
                {
                    Logger.LogInfo($"{CommandLineModel.AppBinaryName} Started on {DateTime.Now} in Service Mode.");
                }
            }
        }
    }
}
