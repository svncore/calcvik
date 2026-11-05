using Avalonia;
using System;

namespace Calcvik;

internal static class Program
{
    public static string? ScreenshotPath { get; private set; }
    public static string? ScreenshotType { get; private set; }
    // Screenshot mode flags
    public static string? ScreenshotMode { get; private set; }
    public static string? ScreenshotTheme { get; private set; }
    public static string? ScreenshotBase { get; private set; }
    public static bool ScreenshotNav { get; private set; }
    public static bool ScreenshotInverse { get; private set; }
    public static string? ScreenshotSwitchMode { get; private set; }
    public static string? ScreenshotInput { get; private set; }
    public static string? ScreenshotExpr { get; private set; }
    public static string? ScreenshotSeq { get; private set; }
    public static bool ScreenshotDump { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--screenshot":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotPath = args[i + 1];
                        i++; // skip value
                    }
                    break;
                case "--mode":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotMode = args[i + 1];
                        i++;
                    }
                    break;
                case "--theme":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotTheme = args[i + 1];
                        i++;
                    }
                    break;
                case "--base":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotBase = args[i + 1];
                        i++;
                    }
                    break;
                case "--nav":
                    ScreenshotNav = true;
                    break;
                case "--inverse":
                    ScreenshotInverse = true;
                    break;
                case "--switch-mode":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotSwitchMode = args[i + 1];
                        i++;
                    }
                    break;
                case "--input":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotInput = args[i + 1];
                        i++;
                    }
                    break;
                case "--expr":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotExpr = args[i + 1];
                        i++;
                    }
                    break;
                case "--seq":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotSeq = args[i + 1];
                        i++;
                    }
                    break;
                case "--dump":
                    ScreenshotDump = true;
                    break;
                case "--type":
                    if (i + 1 < args.Length)
                    {
                        ScreenshotType = args[i + 1];
                        i++;
                    }
                    break;
            }
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
