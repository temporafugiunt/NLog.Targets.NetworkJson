using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.LoadTester.Models;
using NLog.Targets.NetworkJSON.LoadTester.Properties;

namespace NLog.Targets.NetworkJSON.LoadTester
{
    public enum LoadTestCallLogCols
    {
        ClientTime = 0,
        ThreadNumber,
        NumTimesSuccess,
        TotalSuccessTime,
        TotalBytesTransferred,
        AvgSuccessTime,
        AvgBytesPerMs,
        BestBytesPerMs,
        WorstBytesPerMs,
        NumTimesFailed,
        AvgFailedTime,
        LastErrorMessage
    }

    public partial class SimulatedLoggingLoadTeser : Form
    {
        CancellationTokenSource _cts;
        List<Tuple<string, string>> _testDocuments = new List<Tuple<string, string>>();
        string[] _resourceDocumentFileNames;

        private double _totalBytesPerMS;
        private long _totalBytesTransferred;

        public SimulatedLoggingLoadTeser()
        {
            InitializeComponent();
            _resourceDocumentFileNames = new[]
            {
                "TestData0",
                "TestData1",
                "TestData2",
                "TestData3",
                "TestData4",
                "TestData5",
                "TestData6",
                "TestData7",
                "TestData8",
                "TestData9",
                "TestData10",
                "TestData11",
                "TestData12",
                "TestData13",
                "TestData14",
                "TestData15",
                "TestData16",
                "TestData17"
            };
        }

        private async void btnExecuteLoadTest_Click(object sender, EventArgs e)
        {
            int numThreads;
            int numTimesPerThread;

            try
            {
                _totalBytesPerMS = 0;
                _totalBytesTransferred = 0;
                numThreads = int.Parse(txtNumThreadsToRun.Text);
                numTimesPerThread = int.Parse(txtNumTimesToExecute.Text);
                if(numThreads == 0 | numTimesPerThread == 0) throw new Exception("Why you do that?");
            }
            catch
            {
                MessageBox.Show("Can't parse counts... LEARN MATHS!");
                return;
            }

            btnExecuteLoadTest.Enabled = false;
            _cts = new CancellationTokenSource();
            btnCancelLoadTest.Enabled = true;

            try
            {
                await StartLoadTestAsync(numThreads, numTimesPerThread, _cts.Token);
                AddTotalsToGrid(_totalBytesPerMS, _totalBytesTransferred);
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operation was cancelled by the user.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Operation resulted in an exception.\nError: {ex.Message}");
            }

            _cts = null;

            btnCancelLoadTest.Enabled = false;
            btnExecuteLoadTest.Enabled = true;
        }
        
        private void btnClearLog_Click(object sender, EventArgs e)
        {
            dgvLoadTestCallLog.Rows.Clear();
            txtActivityLog.Text = string.Empty;
        }

        private void AddThreadResultsToGrid(LoadTestThreadResults threadInformation)
        {
            var newRow = (DataGridViewRow)dgvLoadTestCallLog.Rows[0].Clone();
            newRow.Cells[(int)LoadTestCallLogCols.ClientTime].Value = threadInformation.ClientStartTime;
            newRow.Cells[(int)LoadTestCallLogCols.ThreadNumber].Value = threadInformation.ThreadID;

            var successCount = threadInformation.CallStats.Count(cs => cs.CallFailed == false);

            newRow.Cells[(int)LoadTestCallLogCols.NumTimesSuccess].Value = successCount;
            if (successCount != 0)
            {
                var successfulCalls = threadInformation.CallStats.Where(cs => cs.CallFailed == false).ToList();
                var totalSuccessTime = successfulCalls.Sum(cs => cs.CallTimeMS);
                var totalSuccessBytes = successfulCalls.Sum(cs => cs.TotalBytes);
                _totalBytesTransferred += totalSuccessBytes;
                var avgBytesPerMs = totalSuccessBytes/(double) totalSuccessTime;
                _totalBytesPerMS += avgBytesPerMs;
                newRow.Cells[(int)LoadTestCallLogCols.AvgSuccessTime].Value = $"{ totalSuccessTime / successCount:0.##} ms";
                newRow.Cells[(int)LoadTestCallLogCols.TotalBytesTransferred].Value = $"{totalSuccessBytes:###,###,###,###,###} Bytes";
                newRow.Cells[(int)LoadTestCallLogCols.BestBytesPerMs].Value = $"{successfulCalls.Max(cs => cs.BytesPerMS):0.##} Bytes / ms";
                newRow.Cells[(int)LoadTestCallLogCols.WorstBytesPerMs].Value = $"{successfulCalls.Min(cs => cs.BytesPerMS):0.##} Bytes / ms";
                newRow.Cells[(int)LoadTestCallLogCols.TotalSuccessTime].Value = $"{totalSuccessTime} ms";
                newRow.Cells[(int)LoadTestCallLogCols.AvgBytesPerMs].Value = $"{ avgBytesPerMs:0.##} Bytes / ms";
            }

            var failureCount = threadInformation.CallStats.Count - successCount;
            newRow.Cells[(int)LoadTestCallLogCols.NumTimesFailed].Value = failureCount;
            if (failureCount != 0)
            {
                var totalFailureTime = threadInformation.CallStats.Where(cs => cs.CallFailed).Sum(cs => cs.CallTimeMS);
                newRow.Cells[(int)LoadTestCallLogCols.AvgFailedTime].Value = $"{totalFailureTime / failureCount:0.##} ms";
            }

            if (threadInformation.LastException != null)
            {
                 newRow.Cells[(int)LoadTestCallLogCols.LastErrorMessage].Value = threadInformation.LastException;
            }
            dgvLoadTestCallLog.Rows.Add(newRow);
        }

        private void AddTotalsToGrid(double totalBytesPerMs, long totalBytesTransferred)
        {
            var newRow = (DataGridViewRow)dgvLoadTestCallLog.Rows[0].Clone();
            newRow.Cells[(int)LoadTestCallLogCols.TotalBytesTransferred].Value = $"{totalBytesTransferred:###,###,###,###,###} Bytes";
            newRow.Cells[(int)LoadTestCallLogCols.AvgBytesPerMs].Value = $"{ totalBytesPerMs:0.##} Bytes / ms";
            dgvLoadTestCallLog.Rows.Add(newRow);
        }

        async Task StartLoadTestAsync(int numThreads, int numTimesPerThread, CancellationToken ct)
        {
            LogActivity($"Setting up {numThreads} test threads.");
            var loadTestThreadDatas = SetupLoadTestThreadList(numThreads, numTimesPerThread);

            // This query when executed will return a collection of tasks.
            var loadTestThreadsQuery = from loadTestThreadData in loadTestThreadDatas select LoadTestManager.RunThreadLoadTestsSimulateLoggingAsync(txtGuaranteedDeliveryDbName.Text, txtNetworkJsonEndpoint.Text, "elastic", loadTestThreadData, ct);

            LogActivity("Loading new threads.");
            // ToList executes the query and start the tasks. 
            List<Task<LoadTestThreadResults>> loadTestThreads = loadTestThreadsQuery.ToList();

            LogActivity("Waiting for thread completions.");
            while (loadTestThreads.Count > 0)
            {
                // Identify the first task that completes.
                Task<LoadTestThreadResults> nextFinishedThread = await Task.WhenAny(loadTestThreads);

                // Remove the selected task from the list so that you don't process it more than once.
                loadTestThreads.Remove(nextFinishedThread);

                // Await the completed task.
                var loadTestThreadResults = await nextFinishedThread;
                AddThreadResultsToGrid(loadTestThreadResults);
            }
        }

        private List<LoadTestThreadData> SetupLoadTestThreadList(int numThreads, int numTimesPerThread)
        {
            var loadTestThreads = new List<LoadTestThreadData>();

            for (var inc = 0; inc < numThreads; inc++)
            {
                loadTestThreads.Add(new LoadTestThreadData(inc, numTimesPerThread, _testDocuments));
            }

            return loadTestThreads;
        }

        private void btnCancelLoadTest_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void DocStorageLoadTesting_Load(object sender, EventArgs e)
        {
            foreach (var resourceDocFileName in _resourceDocumentFileNames)
            {
                _testDocuments.Add(new Tuple<string, string>(resourceDocFileName, Resources.ResourceManager.GetString(resourceDocFileName)));
            }
            txtGuaranteedDeliveryDbName.Text = ConfigurationManager.AppSettings["GdDbPath"];
            txtNetworkJsonEndpoint.Text = ConfigurationManager.AppSettings["NetworkJsonEndpoint"];
        }
        private void LogActivity(string message)
        {
            if (!txtActivityLog.Text.IsNullOrEmpty())
            {
                txtActivityLog.Text = $"{txtActivityLog.Text}\r\n{message}";
            }
            else
            {
                txtActivityLog.Text = message;
            }
        }
    }
}
