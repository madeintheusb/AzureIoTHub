/*
   Copyright (C) 2015 MadeInTheUSB LLC

   The MIT License (MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
  
  https://azure.microsoft.com/en-us/documentation/articles/iot-suite-connecting-devices/
  https://github.com/Azure/azure-iot-sdks
  
  REMOTE MONITORING https://github.com/Azure/azure-iot-remote-monitoring
  
 * Episode 188: IoT Hub with Elio Damaggio and Olivier Bloch
 https://channel9.msdn.com/Shows/Cloud+Cover/Episode-188-IoT-Hub-with-Elio-Damaggio-and-Olivier-Bloch
 Elio Damaggio, Olivier Bloch
  
 Azure/azure-iot-sdks -- https://github.com/Azure/azure-iot-sdks
    See tools - Device Explorer
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureIoTHub_CommandUtility;
using MadeInTheUSB;
using MadeInTheUSB.Adafruit;
using MadeInTheUSB.i2c;
using MadeInTheUSB.GPIO;
using MadeInTheUSB.Sensor;
using MadeInTheUSB.WinUtil;
using Microsoft.Azure.Devices.Client;

namespace LightSensorConsole
{
    class Demo
    {
        private static MCP9808_TemperatureSensor _MCP9808_TemperatureSensor;

        static string GetAssemblyProduct()
        {
            Assembly currentAssem = typeof(Program).Assembly;
            object[] attribs = currentAssem.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
            if(attribs.Length > 0)
                return  ((AssemblyProductAttribute) attribs[0]).Product;
            return null;
        }

        static void Cls(Nusbio nusbio)
        {
            Console.Clear();

            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 8, "Q)uit");

            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.Bar(0, ConsoleEx.WindowHeight-3, string.Format("Nusbio SerialNumber:{0}, Description:{1}", nusbio.SerialNumber, nusbio.Description), ConsoleColor.Black, ConsoleColor.DarkCyan);
        }

        private static bool WaitForSensorsToBeReady(MCP9808_TemperatureSensor mcp9808_TemperatureSensor)
        {
            Console.Clear();
            ConsoleEx.TitleBar(0, GetAssemblyProduct(), ConsoleColor.Yellow, ConsoleColor.DarkBlue);
            ConsoleEx.TitleBar(ConsoleEx.WindowHeight-2, Nusbio.GetAssemblyCopyright(), ConsoleColor.White, ConsoleColor.DarkBlue);
            ConsoleEx.WriteMenu(-1, 4, "Q)uit");

            while (true)
            {
                if (mcp9808_TemperatureSensor.Begin())
                    return true;
                else
                    ConsoleEx.Write(0, 2, string.Format("[{0}]MCP9808 I2C Temperature sensor not connected to Nusbio", DateTime.Now), ConsoleColor.Cyan);

                TimePeriod.Sleep(2*1000);
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true).Key;
                    if (k == ConsoleKey.Q)
                        return false;
                }
            }
        }

        static async Task ReceiveCommands(AzureIoTDevice azureIoTDevice)
        {
            Message receivedMessage;
            string messageData;
            while (true)
            {
                receivedMessage = await azureIoTDevice.DeviceClient.ReceiveAsync();
                if (receivedMessage != null)
                {
                    messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                    var m = string.Format("\t{0}> Received message: {1}", DateTime.Now.ToLocalTime(), messageData);
                    ConsoleEx.WriteLine(0, 7, m.PadRight(79,' '), ConsoleColor.Green); 
                    await azureIoTDevice.DeviceClient.CompleteAsync(receivedMessage);
                }
                Thread.Sleep(2000);
            }
        }

        public static void Run(string[] args)
        {
            Console.WriteLine("Nusbio initialization");
            var serialNumber = Nusbio.Detect();
            
            if (serialNumber == null) // Detect the first Nusbio available
            {
                Console.WriteLine("nusbio not detected");
                return;
            }

            var clockPin           = NusbioGpio.Gpio5; 
            var dataOutPin         = NusbioGpio.Gpio6; 
            var motionSensorPin    = NusbioGpio.Gpio0; 

            using (var nusbio = new Nusbio(serialNumber))
            {
                _MCP9808_TemperatureSensor = new MCP9808_TemperatureSensor(nusbio, dataOutPin, clockPin);
                if (!WaitForSensorsToBeReady(_MCP9808_TemperatureSensor))
                    Environment.Exit(1);

                var motionSensor = new MotionSensorPIR(nusbio, motionSensorPin, 3);

                Cls(nusbio);
                var every5Seconds  = new TimeOut(1000*5);
                var everyHalfSecond = new TimeOut(500);

                ReceiveCommands(AzureIoTHubDevices.TemperatureDevice);

                while(nusbio.Loop())
                {
                    if (everyHalfSecond.IsTimeOut())
                    {
                        var motionType = motionSensor.MotionDetected();
                        if (motionType == MotionSensorPIR.MotionDetectedType.MotionDetected)
                        {
                            ConsoleEx.WriteLine(0, 4, string.Format("[{0}] MotionSensor:{1,-20}", DateTime.Now, motionType), ConsoleColor.DarkCyan);
                            AzureIoTHubDevices.MotionSensorDevice.Update(DateTime.UtcNow);
                        }
                        else if (motionType == MotionSensorPIR.MotionDetectedType.None)
                        {
                            ConsoleEx.Write(0, 4, string.Format("[{0}] MotionSensor:{1,-20}", DateTime.Now, motionType), ConsoleColor.DarkCyan);
                        }
                    }
                    if (every5Seconds.IsTimeOut(isFirstTime:true)) 
                    {
                        double celsius = _MCP9808_TemperatureSensor.GetTemperature(MCP9808_TemperatureSensor.TemperatureType.Celsius);
                        ConsoleEx.WriteLine(0, 5, string.Format("[{0}]Temperature {1:000.00}C, {2:000.00}F, {3:00000.00}K",  DateTime.Now, celsius, _MCP9808_TemperatureSensor.CelsiusToFahrenheit(celsius), _MCP9808_TemperatureSensor.CelsiusToKelvin(celsius) ), ConsoleColor.Cyan);
                        if(AzureIoTHubDevices.TemperatureDevice.ShouldUpdate(celsius))
                            AzureIoTHubDevices.TemperatureDevice.Update(celsius);
                    }
                    if (Console.KeyAvailable)
                    {
                        var k = Console.ReadKey(true).Key;
                        if (k == ConsoleKey.T)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.D0)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.C)
                        {
                            Cls(nusbio);
                        }
                        if (k == ConsoleKey.Q) break;
                        Cls(nusbio);
                    }
                }
            }
            Console.Clear();
        }
    }
}

