using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FYP
{
    internal class Tracking
    {
        public struct MouseLogEntry
        {
            public DateTime Time;
            public int X;
            public int Y;
            public string Action;
        }

        // Public variable to store the latest mouse event
        public static MouseLogEntry LatestMouseEvent { get; private set; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern int GetAsyncKeyState(int vKey);

        private static DateTime lastClickTime = DateTime.MinValue;
        private static bool lastClickWasDouble = false;
        private static readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };


        public static void TrackMouse()
        {
            Console.WriteLine("Mouse tracking and keyboard event capturing. Press the Escape key to stop.");

            // Construct the log path using the current user's username
            string username = Environment.UserName;
            string logPath = $"C:\\Users\\{username}\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Log";
            Directory.CreateDirectory(logPath); // Ensure the directory exists
            string path = Path.Combine(logPath, "mouse_events.csv"); // Name of the log file for mouse events

            using (StreamWriter mouseWriter = new StreamWriter(path, true))
            {
                // Check if the file is empty to determine if headers need to be written
                FileInfo mouseFileInfo = new FileInfo(path);
                if (mouseFileInfo.Length == 0)
                {
                    mouseWriter.WriteLine("Time,X,Y,Action");
                }

                while (true)
                {
                    // Check for mouse events (left-click, right-click, double-click)
                    CheckMouseEvents(mouseWriter);

                    // Sleep for 2000 milliseconds to avoid high CPU usage
                    Thread.Sleep(2000);
                }
            }

            Console.WriteLine("Mouse tracking and keyboard event capturing stopped.");
        }

        private static void CheckMouseEvents(StreamWriter mouseWriter)
        {
            POINT point;
            string eventName = string.Empty; // Initialize the event name to empty

            if (GetCursorPos(out point))
            {
                if ((GetAsyncKeyState(0x01) & 0x8000) != 0) // Left mouse button is down
                {
                    DateTime currentClickTime = DateTime.Now;
                    if ((currentClickTime - lastClickTime).TotalMilliseconds <= 500)
                    {
                        if (!lastClickWasDouble)
                        {
                            eventName = "Double Click";
                            lastClickWasDouble = true;
                        }
                    }
                    else
                    {
                        eventName = "Left Click";
                        lastClickWasDouble = false;
                    }
                    lastClickTime = currentClickTime;
                }
                else if ((GetAsyncKeyState(0x02) & 0x8000) != 0) // Right mouse button is down
                {
                    eventName = "Right Click";
                }
                else
                {
                    if (!lastClickWasDouble)
                    {
                        eventName = "No Click"; // Only log "No Click" if the last event was not a double-click
                    }
                }
            }

            // Reset the lastClickWasDouble if no buttons are pressed
            if ((GetAsyncKeyState(0x01) & 0x8000) == 0 && (GetAsyncKeyState(0x02) & 0x8000) == 0)
            {
                lastClickWasDouble = false;
            }

            // If eventName is empty, it means the last action was a double click and we're still within the double click threshold
            if (string.IsNullOrEmpty(eventName))
            {
                return; // Skip logging this time
            }

            // Log the event, whatever it is
            LatestMouseEvent = new MouseLogEntry { Time = DateTime.Now, X = point.X, Y = point.Y, Action = eventName };
            // Format and write the mouse event data to the file
            string mouseData = $"{LatestMouseEvent.Time:HH:mm:ss},{LatestMouseEvent.X},{LatestMouseEvent.Y},{LatestMouseEvent.Action}";
            mouseWriter.WriteLine(mouseData);
            mouseWriter.Flush(); // Ensure the data is written to the file immediately

            // Preprocess and check for anomalies
            Task.Run(() => CheckForAnomalies(LatestMouseEvent));
        }

        private static async void CheckForAnomalies(MouseLogEntry entry)
        {
            string pythonPath = "C:\\Users\\huzai\\AppData\\Local\\Programs\\Python\\Python311\\python.exe";
            string scriptPath = "C:\\Users\\huzai\\source\\repos\\test1\\mouseanamoly.py";

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = string.Format("{0} {1} {2} {3} \"{4}\"", scriptPath, entry.Time.ToString("o"), entry.X, entry.Y, entry.Action),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.WriteLine($"Python script output: {result}"); // Debug: Print the script output
                    bool isAnomalous;
                    if (bool.TryParse(result.Trim(), out isAnomalous))
                    {
                        if (isAnomalous)
                        {
                            Console.WriteLine("Anomaly detected in mouse data!");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Failed to parse anomaly detection result.");
                    }
                    var messageContent = result.Replace("\r", "").Replace("\n", ""); // Remove carriage returns and newlines
                    var content = new StringContent($"{{ \"source\": \"mouse\", \"message\": \"{messageContent}\" }}", Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync("http://localhost:8080/api/data", content);

                }

                using (StreamReader errorReader = process.StandardError)
                {
                    string error = errorReader.ReadToEnd();
                    if (!string.IsNullOrEmpty(error))
                    {
                        Console.WriteLine($"Error from Python script: {error}");
                    }
                }
            }
        }
    }
}
