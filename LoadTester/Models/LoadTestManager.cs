using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NLog.Targets.NetworkJSON.GuaranteedDelivery;
using NLog.Targets.NetworkJSON.LoadTester.Helper;
using NLog.Targets.NetworkJSON.LoadTester.Loggers;

namespace NLog.Targets.NetworkJSON.LoadTester.Models
{
    public static class LoadTestManager
    {
        public static async Task<LoadTestThreadResults> RunThreadLoadTestsSimulateLoggingAsync(string gdDbPath, string networkJsonEndpoint, string networkJsonEndpointType, LoadTestThreadData threadData, CancellationToken ct)
        {
            Debug.Print("Entered RunThreadLoadTestsAsync.");

            var loadTestResults = new LoadTestThreadResults(threadData.ThreadID) { };

            using (var target = new GDServiceTarget
                {
                    GuaranteedDeliveryDB = gdDbPath,
                    Endpoint = networkJsonEndpoint,
                    EndpointType = networkJsonEndpointType,
                    EndpointExtraInfo = "pv-testapp-loadtest-{0:yyyy.MM.dd}|pv-testapp-loadtest|" + BasicAuthHelper.EncodeToBase64("logstash_writer", "logstash1412$#%*")
                })
            {
                Debug.Print("New GDServiceTarget Created.");

                // Logging Operations
                // =================================
                for (var inc = 0; inc < threadData.NumTimesToExecute; inc++)
                {
                    var rnd = new Random();
                    var rInt = rnd.Next(0, threadData.TestDocuments.Count - 1);
                    var currentTestDocument = threadData.TestDocuments[rInt];
                    var docStorageCallData = new SignalRCallData()
                    {
                        TestDataName = currentTestDocument.Item1,
                        TotalBytes = currentTestDocument.Item2.Length
                    };

                    var sw = Stopwatch.StartNew();
                    try
                    {
                        await target.WriteAsync(currentTestDocument.Item2);
                    
                        sw.Stop();
                        docStorageCallData.CallTimeMS = sw.ElapsedMilliseconds;
                        docStorageCallData.BytesPerMS = docStorageCallData.TotalBytes / (double)docStorageCallData.CallTimeMS;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception)
                    {
                        sw.Stop();
                        docStorageCallData.CallTimeMS = sw.ElapsedMilliseconds;
                        docStorageCallData.CallFailed = true;
                    }
                    loadTestResults.CallStats.Add(docStorageCallData);
                    ct.ThrowIfCancellationRequested();
                }
            }
                

            return loadTestResults;
        }

        public static async Task<LoadTestThreadResults> RunThreadLoadTestsActuallyLoggingAsync(string endpoint, string logStashEndpoint, LoadTestThreadData threadData, CancellationToken ct)
        {
            Debug.Print("Entered RunThreadLoadTestsActuallyLoggingAsync.");

            var loadTestResults = new LoadTestThreadResults(threadData.ThreadID) { };

            // Logging Operations
            // =================================
            for (var inc = 0; inc < threadData.NumTimesToExecute; inc++)
            {
                
                var docStorageCallData = new SignalRCallData();
                var sw = Stopwatch.StartNew();
                try
                {
                    docStorageCallData = LogRandomEvent();

                    sw.Stop();
                    docStorageCallData.CallTimeMS = sw.ElapsedMilliseconds;
                    docStorageCallData.BytesPerMS = docStorageCallData.TotalBytes / (double)docStorageCallData.CallTimeMS;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception)
                {
                    sw.Stop();
                    docStorageCallData.CallTimeMS = sw.ElapsedMilliseconds;
                    docStorageCallData.CallFailed = true;
                }
                loadTestResults.CallStats.Add(docStorageCallData);
                ct.ThrowIfCancellationRequested();
            }

            return loadTestResults;
        }

        public static SignalRCallData LogRandomEvent()
        {
            var biLogger = LoggerFactory.GetBasicInstrumentationLogger();
            var rnd = new Random();
            var randomEvent = rnd.Next(0, 3);
            var callInfo = new SignalRCallData()
            {
                TestDataName = $"RANDOM{randomEvent}"
            };
            biLogger.InitializeExecutionLogging(MethodBase.GetCurrentMethod());
            if (randomEvent == 0)
            {
                callInfo.TotalBytes = 1702; // TODO - Get approximate count from storage (dates will change)
                biLogger.PushDebug("Test Starting");
                biLogger.CurrentPractice = "JamesEby";
                biLogger.PushInfo("Practice was set");

                biLogger.PushInfo("Second Info Statement", "This is some data I want to track but not log just yet");
                biLogger.PushInfo("Third Info Statement", "This is some more data I want to track but not log just yet");
                biLogger.PushDebug("James", "saying hi to Eun");
                biLogger.PushInfoWithTime("Info Time 1");
                biLogger.PushWarnWithTime("Warning Time 2");

                biLogger.PushInfo("last one", "Ok Ready to write to log now");

                biLogger.PushTrace("Gettting ready to log the error.");

                biLogger.PushFatal("Wow, this is a REALLY BAD ERROR!");

                // Log Errors and Infos, everything else should stay in their respective queues.
                biLogger.LogExecutionCompleteAsError(data: "File Error", exceptionInfo: new FileNotFoundException("Couldn't find c:\\blahblah"));
            }
            else if (randomEvent == 2)
            {
                callInfo.TotalBytes = 739; // TODO - Get count from storage
                biLogger.LogInfoWithTime("Finish up the logging test");
            }
            else if (randomEvent == 3)
            {
                callInfo.TotalBytes = 741; // TODO - Get count from storage
                biLogger.LogInfoWithTime("The Info Push stack is clean");
            }
            else
            {
                callInfo.TotalBytes = 1112; // TODO - Get count from storage
                biLogger.PushDebug("Test Starting");
                biLogger.CurrentPractice = "JamesEby";
                biLogger.PushInfo("Practice was set");

                biLogger.PushInfo("Second Info Statement", "This is some data I want to track but not log just yet cuz I'm thinkin!");
                biLogger.LogInfoWithTime("Third Info Statement", "This is some more data I want to track RIGHT NOW!");
            }
            return callInfo;
        }
    }
}
