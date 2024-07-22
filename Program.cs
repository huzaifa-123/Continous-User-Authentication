using System.Threading.Tasks;
using FYP;

class Program
{
    static async Task Main(string[] args)
    {
        // Start the ASP.NET Core server and monitor data.json for changes
        Task serverAndMonitorTask = ServerMonitor.RunServerAndMonitor();

        // Run TrackMouse and TrackKeyboard asynchronously
        Task trackingTask = Task.Run(() => Tracking.TrackMouse());
        Task tracking2Task = Task.Run(() => Tracking2.TrackKeyboard());

        // Add a delay to ensure the monitor task has time to start up
        await Task.Delay(1000);

        // Wait for the server and tracking tasks to complete
        await Task.WhenAll(serverAndMonitorTask, trackingTask, tracking2Task);

        // The program will continue here after all tasks have completed
        Console.WriteLine("All tasks completed.");
    }
}
