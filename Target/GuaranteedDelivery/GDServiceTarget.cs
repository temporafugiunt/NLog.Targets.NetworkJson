using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog.Config;
using NLog.Targets.NetworkJSON.ExtensionMethods;
using NLog.Targets.NetworkJSON.GuaranteedDelivery.LocalLogStorageDB;

namespace NLog.Targets.NetworkJSON.GuaranteedDelivery
{
    [Target("GDService")]
    public class GDServiceTarget : TargetWithLayout
    {
        #region Guaranteed Delivery Service Variables

        private Uri _endpoint;
        private SQLiteConnection _dbConnection;
        private bool _disposed;

        #endregion

        public class GDServiceTypes
        {
            public const string socket = "socket";
            public const string elastic = "elastic";
        }

        #region Task Properties

        /// <summary>
        /// The name of the Guaranteed Delivery DB that will temporarily house this target's log records for discovery and transmission by the GDNetworkJSONService.
        /// </summary>
        [Required]
        public string GuaranteedDeliveryDB { get; set; }

        /// <summary>
        /// The URI of the endpoint, if logstash is the EndpointType, this would be of the format:
        /// 
        /// tcp://d-loga.pvops.com:8889
        /// 
        /// if elastic is the EndpointType, this would be of the format:
        /// 
        /// https://d-loga.pvops.com:9200
        /// 
        /// </summary>
        [Required]
        public string Endpoint
        {
            get { return _endpoint.ToString(); }
            set
            {
                if (value != null)
                {
                    _endpoint = new Uri(Environment.ExpandEnvironmentVariables(value));
                }
                else
                {
                    _endpoint = null;
                }
            }
        }

        /// <summary>
        /// Current supported types at the moment are:
        /// 
        /// "socket"
        /// "elastic"
        /// 
        /// socket = A raw TCP or UDP socket where newline terminated JSON documents are transmitted (such as to Logstash's TCP Input Plugin).
        /// Guaranteed Delivery uses the NetworkJsonTarget to complete transmission of this type but in an ATOMIC way.
        /// 
        /// elastic = A JSON document is transmitted to elastic via Guaranteed Delivery and is written to the
        /// index specified by EndpointExtraInfo. The INDEX_NAME can contain a datetime format for a string format parameter 0 
        /// which can be used to create an index with similar date/time formatting to that of logstash, i.e.:
        /// 
        /// elastic
        /// 
        /// Would be the equivalent to the logstash elastic plugin's default index pattern of "logstash-%{+YYYY.MM.dd}".
        /// 
        /// </summary>
        [Required]
        public string EndpointType { get; set; }

        /// <summary>
        /// Certain EndPoint types require extra information that is housed in this value.
        /// 
        /// When type is "elastic" this will house the index name that the message should be written to. and the document type. Can also OPTIONALLY house
        /// a base 64 encoded user name and password if basic authentication is setup with Elastic.
        /// 
        /// INDEX_NAME|DOCUMENT_TYPE|BASE64_USERNAME_PASSWORD
        /// 
        /// INDEX_NAME and DOCUMENT_TYPE are required, BASE64_USERNAME_PASSWORD is not.
        /// 
        /// The index name can contain a datetime format for a string format parameter 0 
        /// which can be used to create an index with similar date/time formatting to that of logstash, i.e.:
        /// 
        /// logstash-{0:yyyy.MM.dd}
        /// 
        /// Would be the equivalent to the logstash elastic plugin's default index pattern of "logstash-%{+YYYY.MM.dd}".
        /// 
        /// </summary>
        public string EndpointExtraInfo { get; set; }

        private void VerifyDbConnection()
        {
            if (_dbConnection == null)
            {
                if(!File.Exists(GuaranteedDeliveryDB)) VerifyDbDirectory();
                _dbConnection = LogStorageConnection.OpenConnection(GuaranteedDeliveryDB);
                Debug.WriteLine($"Db Connection Created.");
                
                if(!LogStorageTable.TableExists(_dbConnection)) { LogStorageTable.CreateTable(_dbConnection); }
            }
        }

        private void VerifyDbDirectory()
        {
            var fileInfo = new FileInfo(GuaranteedDeliveryDB);
            try
            {
                var dbDirectory = fileInfo.Directory?.FullName;
                if(dbDirectory.IsNullOrEmpty()) throw new Exception();
                if (!Directory.Exists(dbDirectory)) Directory.CreateDirectory(dbDirectory);
            }
            catch 
            {
                throw new Exception($"Unable to create or verify the directory structure for {GuaranteedDeliveryDB}");
            }
        }

        private void CloseDbConnection()
        {
            if (_dbConnection == null) return;
            Debug.WriteLine("DB Connection Closed.");
            _dbConnection.Close();
            _dbConnection.Dispose();
            _dbConnection = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                CloseDbConnection();
            }
            _disposed = true;
            base.Dispose(disposing);
        }

        [ArrayParameter(typeof(ParameterInfo), "parameter")]
        public IList<ParameterInfo> Parameters { get; }
        
        #endregion

        private IConverter Converter { get; }

        public GDServiceTarget() : this(new JsonConverter())
        {
        }

        public GDServiceTarget(IConverter converter)
        {
            Converter = converter;
            this.Parameters = new List<ParameterInfo>();
        }
        
        public void WriteLogEventInfo(LogEventInfo logEvent)
        {
            Write(logEvent);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            foreach (var par in Parameters)
            {
                if (!logEvent.Properties.ContainsKey(par.Name))
                {
                    string stringValue = par.Layout.Render(logEvent);

                    logEvent.Properties.Add(par.Name, stringValue);
                }
            }
            
            var jsonObject = Converter.GetLogEventJson(logEvent);
            if (jsonObject == null) return;
            var jsonObjectStr = jsonObject.ToString(Formatting.None, null);
            Write(jsonObjectStr);
        }

        public void Write(string logEventAsJsonString)
        {
            try
            {
                VerifyDbConnection();
                LogStorageTable.TableExists(_dbConnection);
                LogStorageTable.InsertLogRecord(_dbConnection, Endpoint, EndpointType, EndpointExtraInfo, logEventAsJsonString);
            }
            catch (Exception)
            {
                CloseDbConnection();
                throw;
            }
        }

        public async Task WriteAsync(string logEventAsJsonString)
        {
            try
            {
                VerifyDbConnection();
                LogStorageTable.TableExists(_dbConnection);
                await LogStorageTable.InsertLogRecordAsync(_dbConnection, Endpoint, EndpointType, EndpointExtraInfo, logEventAsJsonString);
            }
            catch (Exception)
            {
                CloseDbConnection();
                throw;
            }
        }
    }
}
