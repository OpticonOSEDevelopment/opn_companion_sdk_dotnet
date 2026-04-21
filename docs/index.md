# Opticon.csp2.net

A .NET wrapper for the Opticon CSP2 library, enabling C# applications to interface with OPN Companion devices.

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
|----------------------|----------|
| .NET Framework 4.6.1+ | ✅ |
| .NET Standard 2.0     | ✅ |
| .NET 6 / .NET 8       | ✅ |

## Usage

The following example demonstrates how to initialize the DLL, poll for devices, and retrieve barcode data.
	
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception occurred: {ex.Message}");
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
