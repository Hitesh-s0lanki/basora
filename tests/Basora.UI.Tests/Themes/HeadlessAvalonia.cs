using Avalonia;
using Avalonia.Headless;

namespace Basora.UI.Tests.Themes;

/// <summary>
/// Brings up just enough Avalonia for a resource URI to resolve.
/// </summary>
/// <remarks>
/// Loading <c>avares://</c> markup goes through the asset loader, which only exists once
/// the platform services are registered. Headless gives that without a display, so these
/// tests run the same on a build agent as on a desktop.
/// </remarks>
internal static class HeadlessAvalonia
{
    private static readonly Lock Gate = new();
    private static bool _started;

    /// <summary>Registers the platform services, once per test run.</summary>
    public static void EnsureStarted()
    {
        lock (Gate)
        {
            if (_started)
            {
                return;
            }

            AppBuilder.Configure<Application>()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions())
                .SetupWithoutStarting();

            _started = true;
        }
    }
}
