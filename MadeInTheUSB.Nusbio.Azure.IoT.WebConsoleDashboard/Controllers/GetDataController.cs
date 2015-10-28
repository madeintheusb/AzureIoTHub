using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Http;
using AzureIoTHub_CommandUtility;

namespace MadeInTheUSB.Nusbio.Azure.IoT.WebConsoleDashboard.Controllers
{
    public class EditorApiResult
    {
        public bool Succeeded {  get; set;}
        public object Data {  get; set;}
        public string Error {  get; set;}
        public List<string> DataList {  get; set;}
        public string Url {  get; set;}

        public EditorApiResult Fail(string error)
        {
            this.SetStatus(false);
            this.Error = error;
            return this;
        }
        public void SetStatus(bool s)
        {
            this.Succeeded = s;
        }

        public EditorApiResult()
        {
            this.Data      = null;
            this.DataList  = null;
            this.Error     = null;
            this.Succeeded = false;
        }

        public string ToJson(bool indented = true)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this, indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None);
            return json;
        }
    }

    public class GetDataController : ApiController
    {
        private static AzureIoTHubMonitor _azureIoTHubMonitor; 

        public HttpResponseMessage Get()
        {
            var deviceId       = string.Empty;
            var action         = string.Empty;
            DateTime startTime = DateTime.Now;
            var lastMaxSample  = 32;
            var r              = new EditorApiResult();
            var nameValues     = base.Request.RequestUri.ParseQueryString();
            r.Url              = base.Request.RequestUri.ToString();

            if (nameValues.AllKeys.Contains("deviceId"))
            {
                deviceId = nameValues["deviceId"];
            }
            if (nameValues.AllKeys.Contains("action"))
            {
                action = nameValues["action"];
            }
            if (nameValues.AllKeys.Contains("startTime"))
            {
                var s = nameValues["startTime"];
                startTime = DateTime.Parse(s);
            }
            if (nameValues.AllKeys.Contains("lastMaxSample"))
            {
                var s = nameValues["lastMaxSample"];
                lastMaxSample = int.Parse(s);
            }
            if (string.IsNullOrEmpty(deviceId))
            {
                r.SetStatus(false);
            }
            else
            {
                //if(action.ToLowerInvariant() =="cleardata")
                  //  _azureIoTHubMonitor.ClearData();

                if (_azureIoTHubMonitor == null || startTime != _azureIoTHubMonitor.StartTime)
                {
                    if (_azureIoTHubMonitor != null)
                        _azureIoTHubMonitor.Clean();
                    _azureIoTHubMonitor = new AzureIoTHubMonitor(null, AzureIoTHubDevices.AllDevices, startTime);
                }
                r.Data = _azureIoTHubMonitor.Monitor(deviceId, lastMaxSample);
                r.SetStatus(r.Data != null);
            }

            return new HttpResponseMessage()
            {
                Content = new StringContent(r.ToJson(), Encoding.UTF8, "text/json")
            };
        }
    }
}