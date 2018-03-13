using System;
using System.Text;
using GDNetworkJSONService.Exceptions;
using GDNetworkJSONService.LocalLogStorageDB;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.GuaranteedDelivery;
using NLog.Targets.NetworkJSON.Helper;
using RestSharp;
using RestSharp.Authenticators;
using tiv.elasticClient.APIs.Index.Enums;
using tiv.elasticClient.APIs._bulk;
using tiv.elasticClient.APIs._bulk.ExtensionMethods;

namespace GDNetworkJSONService.GDEndpointWriters
{
    class ElasticEndpointWriter : IGDEndpointWriter
    {
        private string _index;
        private string _docType;
        private IRestClient _elasticClient;

        public bool AllowMultiWrite => true;

        public ElasticEndpointWriter(string endpoint, string endpointExtraInfo)
        {
            if (endpointExtraInfo.IsNullOrEmpty())
                throw new DeadLetterException((int)DeadLetterLogStorageTable.ArchiveReasonId.MissingExtraInfo);
            var extraInfo = endpointExtraInfo.Split('|');
            IAuthenticator auth = null;
            // Just the destination index and the destination document type.
            if (extraInfo.Length == 2)
            {
                _index = extraInfo[0];
                _docType = extraInfo[1];
            }
            // Destination index has a base 64 encoded basic auth username and password.
            else if (extraInfo.Length == 3)
            {
                _index = extraInfo[0];
                _docType = extraInfo[1];
                var userPassBase64 = BasicAuthHelper.GetBasicAuthUserAndPassword(extraInfo[2]);
                if(!userPassBase64.HasValue) throw new DeadLetterException((int)DeadLetterLogStorageTable.ArchiveReasonId.InvalidExtraInfo);
                auth = new HttpBasicAuthenticator(userPassBase64.Value.Key, userPassBase64.Value.Value);
            }
            else
            {
                throw new DeadLetterException((int)DeadLetterLogStorageTable.ArchiveReasonId.InvalidExtraInfo);
            }
            _elasticClient = tiv.elasticClient.RestClientUtility.Instance.NewClient(endpoint, auth);
        }

        public void Write(string logEventAsJsonString)
        {
            // Bulk of one at the moment... Yes, I know...
            var bulkApi = new BulkAPI();
            var bulkRequestBuilder = new StringBuilder();
            var index = _index;
            // Index contains a date string that should be translated.
            if(_index.Contains("{"))
            {
                index = string.Format(_index, DateTime.Now);
            }
            
            bulkRequestBuilder.AddHitToBulkOperation(IndexDocumentActionType.Index, logEventAsJsonString.Replace(Environment.NewLine, ""), index, _docType);
            var bulkRequestBody = bulkRequestBuilder.ToString();
            string dummy;
            bulkApi.Post(_elasticClient, bulkRequestBody, out dummy);
        }

        public void Write(string[] logEventsAsJsonStrings)
        {
            // Bulk of one at the moment... Yes, I know...
            var bulkApi = new BulkAPI();
            var bulkRequestBuilder = new StringBuilder();
            var index = _index;
            // Index contains a date string that should be translated.
            if (_index.Contains("{"))
            {
                index = string.Format(_index, DateTime.Now);
            }

            foreach (var logEventAsJsonString in logEventsAsJsonStrings)
            {
                bulkRequestBuilder.AddHitToBulkOperation(IndexDocumentActionType.Index, logEventAsJsonString.Replace(Environment.NewLine, ""), index, _docType);
            }
            
            var bulkRequestBody = bulkRequestBuilder.ToString();
            string dummy;
            bulkApi.Post(_elasticClient, bulkRequestBody, out dummy);
        }
    }
}
