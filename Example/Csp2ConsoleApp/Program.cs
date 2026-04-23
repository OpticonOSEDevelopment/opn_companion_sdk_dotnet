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