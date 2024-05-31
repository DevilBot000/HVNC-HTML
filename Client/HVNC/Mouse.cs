using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

internal class Mouse
{
    // Asynchronous method to handle move_mouse command
    public static async Task MoveMouse(string command)
    {
        // We break the command into parts by spaces
        string[] parts = command.Split(' ');

        // We check that the command consists of three parts and starts with “move_mouse”
        if (parts.Length == 3 && parts[0] == "move_mouse")
        {
            int x, y;

            // We are trying to convert the second and third parts of the command to integers (x and y coordinates)
            if (int.TryParse(parts[1], out x) && int.TryParse(parts[2], out y))
            {
                // If parsing is successful, output the coordinates to the console
                Console.WriteLine($"Mouse coordinates: x={x}, y={y}");

                // Save the current cursor coordinates
                int originalX = Cursor.Position.X;
                int originalY = Cursor.Position.Y;

                // We get the coordinates of all monitors
                var allScreens = Screen.AllScreens;
                int totalXOffset = 0;

                // We go through all the monitors except the last one
                for (int i = 0; i < allScreens.Length - 1; i++)
                {
                    // Adding the monitor width to the total X offset
                    totalXOffset += allScreens[i].Bounds.Width;
                }

                // Add the X offset of all monitors to the X coordinate
                x += totalXOffset;

                // Set the cursor to new coordinates
                Cursor.Position = new Point(x, y);

                // Simulate a mouse click (pressing and releasing the left button) in new coordinates
                NativeMethods.mouse_event(NativeMethods.MOUSEEVENTF_LEFTDOWN | NativeMethods.MOUSEEVENTF_LEFTUP, x, y, 0, UIntPtr.Zero);

                // Adding a small delay
                await Task.Delay(100);

                // Returning the cursor to the original coordinates
                Cursor.Position = new Point(originalX, originalY);
            }
            else
            {
                // If coordinate parsing fails, display an error message
                Console.WriteLine("Error parsing coordinates.");
            }
        }
        else
        {
            // If the command does not match the format "move_mouse x y", an error message is displayed
            Console.WriteLine("Incorrect move_mouse command format.");
        }
    }
}

