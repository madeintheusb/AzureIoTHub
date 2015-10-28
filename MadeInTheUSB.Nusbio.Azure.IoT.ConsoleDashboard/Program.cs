using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureIoTHub_CommandUtility;
using Microsoft.Azure.Devices.Common;
using Microsoft.ServiceBus.Messaging;

namespace MadeInTheUSB.Nusbio.Azure.IoT.ConsoleDashboard
{
    class Program
    {
        private static void Cls()
        {
            Console.Clear();
            Console.WriteLine("Command Q)uit ");
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Monitor Devices From {0}", AzureIoTHubDevices.HostName);
            var ctsForDataMonitoring = new CancellationTokenSource();
            Cls();
            var azureIoTHubMonitor = new AzureIoTHubMonitor(ctsForDataMonitoring, AzureIoTHubDevices.AllDevices);
            while (true)
            {
                var result = azureIoTHubMonitor.Monitor("");
                if (result != null)
                {
                    foreach (var r in result)
                        Console.WriteLine(r.ToString());
                }

                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey().Key;
                    if (k == ConsoleKey.Q)
                    {
                        azureIoTHubMonitor.Stop();
                        break;
                    }
                    if (k == ConsoleKey.C) Cls();
                }
            }
        }
    }
}
