namespace BeaconBridge.Startup.Web;

public static class WebEntrypoint
{
  public static async Task Run(string[] args)
  {
    var b = WebApplication.CreateBuilder(args);

    // Configure DI Services
    b.ConfigureServices();

    // Build the app
    var app = b.Build();

    // Configure the HTTP Request Pipeline
    app.UseWebPipeline();

    // Seed DB cache
    await app.Initialise();

    // Run the app!
    await app.RunAsync();
  }
}
