using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MadeInTheUSB;

namespace AzureIoTHub_CommandUtility
{
    /// <summary>
    /// https://azure.microsoft.com/en-us/documentation/articles/iot-hub-csharp-csharp-getstarted/
    /// https://azure.microsoft.com/en-us/azurecon/#sessions   
    /// https://www.nuget.org/packages/Microsoft.Azure.Devices/1.0.0-preview-002 
    /// https://www.nuget.org/packages?q=Microsoft.Azure.Devices
    /// Install-Package Microsoft.Azure.Devices -Pre
    /// Install-Package Microsoft.Azure.Devices.Client -Pre
    /// AZURE IOTSUITE https://azure.microsoft.com/en-us/solutions/iot/
    /// >>> https://www.azureiotsuite.com/
    /// </summary>
    public class AzureIoTHubDevices
    {
        public static string ConnectionString = "HostName=HouseTemperature.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=yr8wXn392TItoZxyKP0yIyBeF3oNPaL11kDvC9aY8q0=";

        public static string HostName              = "HouseTemperature.azure-devices.net";

        public static string TempSensorDeviceId    = "pcBasementTemperatureSensor";
        public static string TempSensorDeviceKey   = "YxP2O/JRMpzfW+M6fKc0F5eYfM5kEq597S/U4+PO3gQ=";

        public static string MotionSensorDeviceId  = "pcBasementMotionSensor";
        public static string MotionSensorDeviceKey = "qWKvaIceLpVVbCIXsg8EqnA07W+fz9NLz+5Kj2l3b2s=";

        public static TemperatureSensorAzureIoTDevice TemperatureDevice = new TemperatureSensorAzureIoTDevice(
                hubHostName: AzureIoTHubDevices.HostName,
                deviceId   : AzureIoTHubDevices.TempSensorDeviceId, 
                deviceKey  : AzureIoTHubDevices.TempSensorDeviceKey
                );

         public static MotionSensorAzureIoTDevice MotionSensorDevice = new MadeInTheUSB.MotionSensorAzureIoTDevice(
                hubHostName: AzureIoTHubDevices.HostName,
                deviceId   : AzureIoTHubDevices.MotionSensorDeviceId, 
                deviceKey  : AzureIoTHubDevices.MotionSensorDeviceKey
                );

        public static List<AzureIoTDevice> AllDevices = new List<AzureIoTDevice>()
        {
            TemperatureDevice,
            MotionSensorDevice
        };
    }
}
