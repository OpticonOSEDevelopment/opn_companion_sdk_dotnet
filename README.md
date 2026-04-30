# OPN Companion SDK for developing .NET application

This is the official .NET SDK from Opticon Sensors Europe BV for the OPN Companion Scanners to create C# applications to interface with OPN Companion devices using the Nuget Opticon.csp2 package

## Features

* Provides a C# interface for communicating with Opticon Companion devices using the CSP2 native functionality.
* Supports .NET Standard 2.0/2.1, .NET 6, .NET 8, and .NET Framework 4.6.1+.
* Compatible with WinForms, WPF, Console applications, and Windows Services.
* Simplifies device connection, polling, barcode reading, and parameter management

## Installation

Install via NuGet:

```bash
dotnet add package Opticon.csp2
```

Or search for 'Opticon.csp2' in the Visual Studio NuGet Package Manager.

## Compatibility

| Platform              | Supported |
|-----------------------|-----------|
| .NET Framework 4.6.1+ | ✅ |
| .NET Standard 2.0     | ✅ |
| .NET 6 / .NET 8       | ✅ |

## API

The documentation of the API can be found here: [API](https://opticonosedevelopment.github.io/opn_companion_sdk_dotnet/api/Opticon.html)

## Usage

The following example demonstrates how to poll for devices, retrieve barcode data and manage device parameters.
	
```csharp
using Opticon;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine(String.Format("Csp2 DLL Version = {0}", OpnEnvironment.GetDllVersion()));

            var connectedDevices = new HashSet<int>();   // Used to avoid duplicate connection messages

            OpnDevice.StartPolling(device =>
            {
                try
                {
                    if (device.IsConnected)
                    {
                        string deviceId = device.GetDeviceId();
                        string model = device.GetModel();

                        // Handle new connection
                        if (connectedDevices.Add(device.Port))
                        {
                            Console.WriteLine($"[{model}] [{deviceId}] [COM{device.Port}] Connected ({device.GetSoftwareVersion()})");
                        }

                        // Handle barcode data
                        if (device.IsDataAvailable)
                        {
                            // Read all barcodes from the device and store them in a list
                            var barcodes = device.ReadBarcodes();

                            Console.WriteLine($"[{device.GetModel()}] [{deviceId}] [COM{device.Port}] {barcodes.Count} Barcode(s) Read");

                            foreach (var barcode in barcodes)
                            {
                                Console.WriteLine($"[{device.GetModel()}] [{deviceId}] [COM{device.Port}] [{barcode.Timestamp}] [{barcode.Data}] [{barcode.SymbologyName}]");
                            }

                            device.ClearBarcodes();
                        }

                        // Demonstrates the reading and writing of all parameter types (bool, int, enum and string/byte array)
                        device.GetParameter(OpnParameter.Code39, out bool enabled);

                        device.GetParameter(OpnParameter.ScannerOnTime, out int time);

                        device.GetParameter(OpnParameter.DeleteEnable, out DeleteEnableOptions deleteOptions);

                        device.SetParameter(OpnParameter.Code39, true);

                        device.SetParameter(OpnParameter.ScannerOnTime, 20);

                        device.SetParameter(OpnParameter.Gs1DataBar, Gs1DataBarOptions.Gs1DataBar | Gs1DataBarOptions.Gs1Expanded);

                        device.SetParameter(OpnParameter.ScratchPad, "Hello");

                        device.GetTime(out DateTime dTime);

                        device.SetTime(DateTime.Now);       // Sync device time with PC time
                    }
                    else
                    {
                        // Handle disconnect
                        if (connectedDevices.Remove(device.Port))
                        {
                            Console.WriteLine($"[{device.GetModel()}] [{device.GetDeviceId()}] [COM{device.Port}] Disconnected");
                        }
                    }
                    return 1; // Return 1 to indicate the device was successfully processed
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    return 0; // Return 0 to continue polling, so we can retry later
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start polling: {ex.Message}");
        }

        Console.ReadLine();
    }
}
```
The next example demonstrates how to use a multi-threaded approach to poll for devices and read barcodes without blocking the main thread.

```csharp
using Opticon;
using System.Collections.Concurrent;

class Program
{
    enum WorkerState
    {
        Idle,
        Running,
        Success,
        Failed
    }

    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine($"Csp2 DLL Version = {OpnEnvironment.GetDllVersion()}");

            var workerStates = new ConcurrentDictionary<int, WorkerState>();

            OpnDevice.StartPolling(device =>
            {
                try
                {
                    if (device.IsConnected)
                    {
                        // Start worker if none is running
                        if (workerStates.GetOrAdd(device.Port, WorkerState.Idle) == WorkerState.Idle || workerStates[device.Port] == WorkerState.Failed)
                        {
                            workerStates[device.Port] = WorkerState.Running;

                            Task.Run(() =>
                            {
                                try
                                {
                                    string deviceId = device.GetDeviceId();
                                    string model = device.GetModel();

                                    if (workerStates[device.Port] == WorkerState.Running) 
                                    {
                                        Console.WriteLine($"[{model}] [{deviceId}] [COM{device.Port}] Connected ({device.GetSoftwareVersion()})");
                                    }

                                    if (device.IsDataAvailable)
                                    {
                                        int cnt = 0;

                                        foreach (var barcode in device.EnumerateBarcodes())
                                        {
                                            Console.WriteLine($"[{++cnt}][{model}] [{deviceId}] [COM{device.Port}] [{barcode.Timestamp}] [{barcode.Data}] [{barcode.SymbologyName}]");
                                        }
                                    }

                                    device.SetTime(DateTime.Now);

                                    device.ClearBarcodes();

                                    workerStates[device.Port] = WorkerState.Success;
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Worker exception (COM{device.Port}): {ex.Message}");
                                    workerStates[device.Port] = WorkerState.Failed;
                                }
                            });
                        }

                        // Decide return value based on worker state
                        if (workerStates.TryGetValue(device.Port, out var state))
                        {
                            return state == WorkerState.Success ? 1 : 0;
                        }

                        return 0;
                    }
                    else
                    {
                        if (workerStates.TryRemove(device.Port, out _))
                        {
                            Console.WriteLine($"[{device.GetModel()}] [{device.GetDeviceId()}] [COM{device.Port}] Disconnected");
                        }
                        return 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
                    return 0;
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start polling: {ex.Message}");
        }

        Console.ReadLine();
    }
}
```

## Troubleshooting

### Runtime Errors / DLL Not Found

This NuGet package of the CSP2 .NET wrapper should automatically add the correct native Csp2.dll to your output directory. If any DLL errors occur, verify that the correct native Csp2.dll is present in your application's output directory. Ensure the native binary matches your target architecture (x86/x64).

## License

This project is licensed under the MIT License.
