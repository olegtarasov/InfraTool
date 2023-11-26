using System.Diagnostics;
using Common.Contracts.Helpers;
using Common.Contrib.ServiceInstaller;
using Common.Host.Web;
using UpdateWatcher;

if (args.Length > 0 && args[0] == "install")
{
    string? fileName = Process.GetCurrentProcess().MainModule?.FileName;
    if (fileName.IsNullOrEmpty())
    {
        Console.Write("Error: could not get path to the executable");
        return 1;
    }

    var installer = new SystemDServiceInstaller(new()
                                                {
                                                    FileName = fileName,
                                                    EnvironmentVariables = new []
                                                                           {
                                                                               "ASPNETCORE_ENVIRONMENT=Production",
                                                                               "ASPNETCORE_URLS=http://*:5015"
                                                                           },
                                                    Name = "UpdateWatcher",
                                                    DisplayName = "UpdateWatcher",
                                                    Start = StartType.Auto
                                                });

    try
    {
        await installer.RegisterSystemDService();
    }
    catch (Exception e)
    {
        Console.WriteLine("Error registering systemd service. Maybe sudo is missing?\n" + e);
        return 1;
    }

    return 0;
}

return WebApp.Create(args)
    .AddConfigurator(new AppConfigurator())
    .RunWithExitCode();