using Avalonia;

namespace Basora.App;

/// <summary>
/// Process entry point.
/// </summary>
internal static class Program
{
    // Initialisation code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't
    // initialised yet and stuff will break.
    [STAThread]
    private static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// Avalonia configuration. Must stay public and keep this name — the XAML previewer
    /// and designer locate it by convention.
    /// </summary>
    /// <returns>The configured <see cref="AppBuilder"/>.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
