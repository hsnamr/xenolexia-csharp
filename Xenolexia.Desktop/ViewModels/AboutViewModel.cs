using System.Reflection;

namespace Xenolexia.Desktop.ViewModels;

public class AboutViewModel : ViewModelBase
{
    public string Version { get; } = GetVersion();

    private static string GetVersion()
    {
        var asm = Assembly.GetExecutingAssembly();
        var version = asm.GetName().Version;
        return version != null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : "Version 1.0.0";
    }
}
