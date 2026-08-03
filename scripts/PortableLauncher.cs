using System;
using System.Diagnostics;
using System.IO;

internal static class PortableLauncher
{
    [STAThread]
    private static void Main()
    {
        var root = AppDomain.CurrentDomain.BaseDirectory;
        var app = Path.Combine(root, "app");
        var executable = Path.Combine(app, "MunchPet.Runtime.exe");
        var start = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = "--path \"" + app + "\"",
            WorkingDirectory = app,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        start.EnvironmentVariables["DOTNET_ROOT"] = Path.Combine(root, "runtime");
        start.EnvironmentVariables["DOTNET_CLI_TELEMETRY_OPTOUT"] = "1";
        Process.Start(start);
    }
}
