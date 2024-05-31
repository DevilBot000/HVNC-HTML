using System;
using System.Threading.Tasks;
using WindowsInput.Native;
using WindowsInput;

internal class Keyboard
{
    public static async Task PressKey(string command)
    {
        string keyCodeString = command.Substring("press_key ".Length);
        if (int.TryParse(keyCodeString, out int keyCode))
        {
            Console.WriteLine("Key pressed code: " + keyCode);
            SimulateKeyPress(keyCode);
        }
        else
        {
            Console.WriteLine("Failed to recognize key code: " + keyCodeString);
        }
    }

    static void SimulateKeyPress(int keyCode)
    {
        var simulator = new InputSimulator();
        // Conversion keyCode в VirtualKeyCode
        VirtualKeyCode key = (VirtualKeyCode)keyCode;
        // Keystroke emulation
        simulator.Keyboard.KeyPress(key);
    }
}

