using System;
using System.Collections.Generic;

namespace NLog.Targets.NetworkJSON.LoadTester.Models
{
    public class LoadTestThreadData
    {
        public int ThreadID { get; }
        public int NumTimesToExecute { get; }

        public List<Tuple<string, string>> TestDocuments { get; } = new List<Tuple<string, string>>();

        public LoadTestThreadData(int threadID, int numTimesToExecute, IEnumerable<Tuple<string, string>> testDocuments)
        {
            ThreadID = threadID;
            NumTimesToExecute = numTimesToExecute;
            foreach (var testDocument in testDocuments)
            {
                TestDocuments.Add(new Tuple<string, string>(testDocument.Item1, testDocument.Item2));
            }
        }
    }
}
