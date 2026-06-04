using Opticon.Csp2Net;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine($"Csp2Net Package Version = {OpnEnvironment.GetDllVersion()}");

            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
                Console.WriteLine($"\nMake sure your device is configured to use the CDC-driver (default: BQZ; OPN-2500/OPN-6000 only)\n(See https://opticonfigure.opticon.com)\n");

            var connectedDevices = new HashSet<string>();   // Used to avoid duplicate connection messages

            OpnDevice.StartPolling((device, connected) =>
            {
                try
                {
                    if (connected)
                    {
                        device.Connect();

                        device.Interrogate(); // Retrieve additional information about the device

                        string deviceId = device.GetDeviceId() ?? "?";
                        string model = device.GetModel();

                        // Handle new connection
                        if (connectedDevices.Add(device.PortName))
                        {
                            Console.WriteLine($"[{model}] [{deviceId}] [{device.PortName}] Connected ({device.GetSoftwareVersion()})");
                        }

                        // Handle barcode data
                        if (device.IsDataAvailable)
                        {
                            // Read all barcodes from the device and store them in a list
                            var barcodes = device.ReadBarcodes();

                            Console.WriteLine($"[{device.GetModel()}] [{deviceId}] [{device.PortName}] {barcodes.Count} Barcode(s) Read");

                            foreach (var barcode in barcodes)
                            {
                                Console.WriteLine($"[{device.GetModel()}] [{deviceId}] [{device.PortName}] [{barcode.Timestamp}] [{barcode.Data}] [{barcode.SymbologyName}]");
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

                        device.TryGetTime(out DateTime dTime);

                        device.SetTime(DateTime.Now);       // Sync device time with PC time

                        // Don't call disconnect to receive a new call back when data becomes available (Windows only)
                        //device.Disconnect();        
                    }
                    else
                    {
                        // Handle disconnect
                        if (connectedDevices.Remove(device.PortName))
                        {
                            Console.WriteLine($"[{device.GetModel()}] [{device.GetDeviceId()}] [{device.PortName}] Disconnected");
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