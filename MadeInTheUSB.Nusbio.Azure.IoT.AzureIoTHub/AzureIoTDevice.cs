using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureIoTHub_CommandUtility;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using MadeInTheUSB;

namespace MadeInTheUSB
{
    
    public class AzureIoTDevice
    {
        [JsonIgnoreAttribute]
        public DeviceClient DeviceClient;

        [JsonIgnoreAttribute]
        public string DeviceId;
        
        public AzureIoTDevice(string hubHostName, string deviceId, string deviceKey)
        {
            this.DeviceClient = DeviceClient.Create(hubHostName, new DeviceAuthenticationWithRegistrySymmetricKey(deviceId, deviceKey));
            this.DeviceId = deviceId;
        }

        public async Task SendDataToCloud()
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(this);
            await SendDataToCloud(json);
        }

        public async Task SendDataToCloud(string jsonMessageString)
        {
            var message = new Message(Encoding.ASCII.GetBytes(jsonMessageString));
            await DeviceClient.SendEventAsync(message);
            //ConsoleEx.WriteLine(0, 6, string.Format("[{0}] Sending message: {1}", DateTime.Now, jsonMessageString), ConsoleColor.Yellow);
        }

        public async Task SendDataToCloud(string property, object value)
        {
            await SendDataToCloud(new Dictionary<string, object> ()
            {
                { property, value}
            });
        }

        internal static T Deserialize<T>(string json) where T : new()
        {
            T t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
            return t;
        }

        public async Task SendDataToCloud(Dictionary<string, object> nameValues)
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(nameValues, Newtonsoft.Json.Formatting.None);
            await SendDataToCloud(json);
        }
    }

    public class TemperatureSensorAzureIoTDevice : AzureIoTDevice
    {
        public double Temperature;
        private const double UpdateOffsetDefault = 0.8;
        /// <summary>
        /// Send new temperature to the cloud if the different between old and new value is greather than
        /// </summary>
        private double UpdateOffset = UpdateOffsetDefault;

        public TemperatureSensorAzureIoTDevice(string hubHostName, string deviceId, string deviceKey, double updateOffset = UpdateOffsetDefault) :base (hubHostName, deviceId, deviceKey)
        {
            this.UpdateOffset = updateOffset;
        }
        public bool ShouldUpdate(double newTemperatureValue)
        {
            var d = 100-(this.Temperature/newTemperatureValue*100);
            return ((d == 0)&&(this.Temperature != newTemperatureValue)) || Math.Abs(d) > this.UpdateOffset;
        }
        public async Task Update(double temperature)
        {
            this.Temperature = temperature;
            await base.SendDataToCloud();
        }
    }

    public class MotionSensorAzureIoTDevice : AzureIoTDevice
    {
        public DateTime MotionDetectedUtcTimeStamp;

        public async Task Update(DateTime motionDetectedUtcTimeStamp)
        {
            this.MotionDetectedUtcTimeStamp = motionDetectedUtcTimeStamp;
            await base.SendDataToCloud();
        }
        public MotionSensorAzureIoTDevice(string hubHostName, string deviceId, string deviceKey) :base (hubHostName, deviceId, deviceKey)
        {            
        }
    }
}
