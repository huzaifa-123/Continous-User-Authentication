using Microsoft.AspNetCore;
using System.ServiceProcess;
using WebsiteTracker;

namespace FYP
{
    public class FypServer
    {
        public partial class Server : ServiceBase
        {
            private readonly ConfigurationService _configurationService;
            private AppConfig _appConfig;
            private IWebHost _host;

            public Server()
            {
                _configurationService = new ConfigurationService();
            }

            protected override void OnStart(string[] args)
            {
                string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                _appConfig = _configurationService.LoadConfiguration(configFilePath);

                // Start the web server
                _host = CreateHostBuilder(args).Build();
                _host.Start();

                // Display a message indicating that the server is up
                Console.WriteLine("Server is running on http://localhost:5021/");
            }

            protected override void OnStop()
            {
                // Stop the web server
                _host?.StopAsync().Wait();
                _host?.Dispose();
                _host = null;
            }

            private IWebHostBuilder CreateHostBuilder(string[] args) =>
             WebHost.CreateDefaultBuilder(args)
            .UseStartup<Startup>()
            .UseUrls("http://localhost:5021/");

        }


    }
}
