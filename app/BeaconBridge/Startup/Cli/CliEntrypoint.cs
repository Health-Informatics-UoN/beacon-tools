using System.CommandLine;

namespace BeaconBridge.Startup.Cli;

public class CliEntrypoint : RootCommand
{
  public CliEntrypoint() : base("Beacon to HutchAgent bridge")
  {
    AddGlobalOption(new Option<string>(new[] { "--environment", "-e" }));

    // Add Commands here
  }
}
