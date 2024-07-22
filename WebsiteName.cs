using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Newtonsoft.Json;
using System.Text.Json;
using System.Globalization;
using System.Text;
using WebsiteTracker;
    
namespace FYP
{
    public class WebsiteEntry
    {
        public string WebsiteName { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

    public class CsvWebsiteEntry
    {
        [Name("Website Name")]
        public string WebsiteName { get; set; }
    }

    public static class ServerMonitor
    {
        private static WebsiteEntry _latestWebsiteEntry;
        private static readonly object _lock = new object();
        private static readonly HttpClient httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080") };


        public static async Task RunServerAndMonitor()
        {
            // Start the ASP.NET Core server
            IHost server = CreateHostBuilder().Build();
            Task serverTask = server.StartAsync();

            // Monitor data.json for changes
            Task monitorTask = Task.Run(() => MonitorDataFile("data.json"));

            // Wait for the server and monitoring tasks to complete
            await Task.WhenAll(serverTask, monitorTask);

            // Stop the ASP.NET Core server
            await server.StopAsync();
        }

        public static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("http://localhost:5021/");
                });

        private static async Task MonitorDataFile(string filePath)
        {
            while (true)
            {
                await Task.Delay(5000); // Check every 5 seconds for changes

                if (File.Exists(filePath))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(filePath);
                        var websiteEntries = JsonConvert.DeserializeObject<List<WebsiteEntry>>(json);

                        if (websiteEntries != null && websiteEntries.Count > 0)
                        {
                            lock (_lock)
                            {
                                _latestWebsiteEntry = websiteEntries[websiteEntries.Count - 1];
                                CheckForAnomaly(_latestWebsiteEntry.WebsiteName, "C:\\Users\\huzai\\Downloads\\websites.csv");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error reading or deserializing file: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("File not found: " + filePath);
                }
            }
        }

        private static async Task CheckForAnomaly(string latestWebsiteName, string csvFilePath)
        {
            try
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                };

                using (var reader = new StreamReader(csvFilePath))
                using (var csv = new CsvReader(reader, config))
                {
                    var websiteEntries = csv.GetRecords<CsvWebsiteEntry>().ToList();

                    var websiteNames = websiteEntries.Select(entry => entry.WebsiteName).ToList();

                    if (!websiteNames.Contains(latestWebsiteName))
                    {
                        Console.WriteLine("Anomaly detected: " + latestWebsiteName);
                        string message = $"Anomaly detected: {latestWebsiteName}";

                        // Construct the JSON payload
                        var payload = new
                        {
                            source = "network",
                            type = "anomaly",
                            message = message,
                        };

                        // Serialize payload to JSON
                        string jsonPayload = System.Text.Json.JsonSerializer.Serialize(payload);

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
            catch (Exception ex)
            {
                Console.WriteLine("Error reading or processing CSV file: " + ex.Message);
            }
        }
    }
}
