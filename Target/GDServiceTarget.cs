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
using NLog.Targets.NetworkJSON.LocalLogStorageDB;

namespace NLog.Targets.NetworkJSON
{
    [Target("GDService")]
    public class GDServiceTarget : TargetWithLayout
    {
        #region Guaranteed Delivery Service Variables

        private Uri _networkJsonEndpoint;
        private SQLiteConnection _dbConnection;
        private bool _disposed;

        #endregion

        #region Task Properties

        [Required]
        public string GuaranteedDeliveryDB { get; set; }

        [Required]
        public string NetworkJsonEndpoint
        {
            get { return _networkJsonEndpoint.ToString(); }
            set
            {
                if (value != null)
                {
                    _networkJsonEndpoint = new Uri(Environment.ExpandEnvironmentVariables(value));
                }
                else
                {
                    _networkJsonEndpoint = null;
                }
            }
        }

        [Required]
        public string NetworkJsonType { get; set; }

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
                LogStorageTable.InsertLogRecord(_dbConnection, NetworkJsonEndpoint, NetworkJsonType, logEventAsJsonString);
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
                await LogStorageTable.InsertLogRecordAsync(_dbConnection, NetworkJsonEndpoint, NetworkJsonType, logEventAsJsonString);
            }
            catch (Exception)
            {
                CloseDbConnection();
                throw;
            }
        }
    }
}
