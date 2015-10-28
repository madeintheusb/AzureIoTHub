using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureIoTHub_CommandUtility;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using MadeInTheUSB;

namespace MadeInTheUSB
{
    public class AzureIoTHubMonitor
    {
        CancellationTokenSource                      _cancellationTokenSource;
        List<AzureIoTDevice>                         _allDevices;
        EventHubClient                               _eventHubClient;
        Dictionary<AzureIoTDevice, EventHubReceiver> _eventHubReceivers = new Dictionary<AzureIoTDevice, EventHubReceiver>();
        public bool FailedState;
        public DateTime StartTime;

        private int _internalCallCount = 0;

        /// <summary>
        /// On the first IoTHub call is takes more time to retreive the data so we set a
        /// specific time out
        /// </summary>
        public static int FIRST_CALL_TIMEOUT_SECONDS = 3;
        private Dictionary<string, List<MonitoringResult>> _monitoringResults = null;

        public static int MAX_SAMPLE_TO_ASK_FOR_SINCE_START_TIME = 10000;

        public AzureIoTHubMonitor(CancellationTokenSource cancellationTokenSource, List<AzureIoTDevice> allDevices, DateTime? startTime = null)
        {
            this.StartTime                = startTime.HasValue ? startTime.Value:DateTime.Now;
            this._allDevices              = allDevices;
            var consumerGroupName         = "$Default";
            this._cancellationTokenSource = cancellationTokenSource;
            _eventHubClient               = EventHubClient.CreateFromConnectionString(AzureIoTHubDevices.ConnectionString, "messages/events");
            var eventHubPartitionsCount   = _eventHubClient.GetRuntimeInformation().PartitionCount;

            foreach (var d in this._allDevices)
            {
                var partition = EventHubPartitionKeyResolver.ResolveToPartition(d.DeviceId, eventHubPartitionsCount);
                _eventHubReceivers.Add(d, _eventHubClient.GetConsumerGroup(consumerGroupName).CreateReceiver(partition, this.StartTime));
            }
        }

        public void Stop()
        {
            this._cancellationTokenSource.Cancel();
            this.Clean();
        }

        public void Clean()
        {
            if (_eventHubReceivers != null)
            {
                foreach (var e in _eventHubReceivers)
                    _eventHubReceivers[e.Key].Close();
                _eventHubReceivers = null;
            }
            if (_eventHubClient != null)
            {
                _eventHubClient.Close();
                _eventHubClient = null;
            }
        }

        public class MonitoringResult
        {
            public string DeviceId;
            public Dictionary<string, object> Properties;
            public DateTime EnqueuedTime;

            public override string ToString()
            {
                return string.Format("[{0}] time:{1}, properties.Count:{2}",
                    this.DeviceId, this.EnqueuedTime, this.Properties.Count);
            }
        }

        

        /// <summary>
        /// Query all the monitored devices for data received by the hub
        /// </summary>
        /// <returns></returns>
        public List<MonitoringResult> Monitor(string deviceId, int lastMaxSample = -1, int timeOut = 1)
        {
            if (_internalCallCount == 0)
                timeOut = FIRST_CALL_TIMEOUT_SECONDS;

            if (_monitoringResults == null)
                _monitoringResults = new Dictionary<string, List<MonitoringResult>>();

            try
            {
                int i = 0;
                while(i < 2) // On the first call we need to execute the loop twice for some unknown Azure IoTHub
                {
                    foreach (var e in _eventHubReceivers)
                    {
                        if (deviceId == e.Key.DeviceId)
                        {
                            // TODO: we need to get all the data from the start time
                            var eventDatas = _eventHubReceivers[e.Key].Receive(MAX_SAMPLE_TO_ASK_FOR_SINCE_START_TIME, TimeSpan.FromSeconds(timeOut));
                            foreach (var eventData in eventDatas)
                            {
                                var data = Encoding.UTF8.GetString(eventData.GetBytes());
                                DateTime enqueuedTime = eventData.EnqueuedTimeUtc.ToLocalTime();

                                var mr = new MonitoringResult {DeviceId = e.Key.DeviceId, EnqueuedTime = enqueuedTime};
                                mr.Properties = AzureIoTDevice.Deserialize<Dictionary<string, object>>(data);

                                if (!_monitoringResults.ContainsKey(deviceId))
                                    _monitoringResults.Add(deviceId, new List<MonitoringResult>());
                                _monitoringResults[deviceId].Add(mr);
                            }
                            if (this._cancellationTokenSource != null)
                                this._cancellationTokenSource.Token.ThrowIfCancellationRequested();
                        }
                    }
                    i++;
                    if (_internalCallCount == 0)
                        Thread.Sleep(1000); // Looks like waiting 1 second and re trying make a difference
                    else
                        break;
                }
            }
            catch (Exception ex)
            {
                if (this._cancellationTokenSource != null && this._cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Console.WriteLine("Exiting monitoring"); // Exit mode
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                    FailedState = true;
                }
            }
            _internalCallCount++;

            if (_monitoringResults.ContainsKey(deviceId))
            {
                // The data is order by : if we ask the 32 since the specified startTime
                // We get the first 32 since the specified startTime and not the last
                // For assume that we got all the sample since the specified startTime and
                // and only key the LAST lastMaxSample
                while (_monitoringResults[deviceId].Count > lastMaxSample)
                {
                    //_monitoringResults[deviceId].RemoveAt(_monitoringResults[deviceId].Count-1);
                    _monitoringResults[deviceId].RemoveAt(0);
                }
                return _monitoringResults[deviceId];
            }
            else 
                return new List<MonitoringResult>();
        }
    }
}
