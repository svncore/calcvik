using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Calcvik.Models;
using Calcvik.Services;
using Calcvik.ViewModels;
using Calcvik.Views;

namespace Calcvik;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            if (window.DataContext is CalculatorViewModel vm)
            {
                if (Program.ScreenshotTheme is { } t && Enum.TryParse<AppTheme>(t, true, out var th)) vm.SelectedTheme = th;
                if (Program.ScreenshotMode is { } m && Enum.TryParse<CalculatorMode>(m, true, out var md)) vm.SelectedMode = md;
                if (Program.ScreenshotNav) vm.IsNavOpen = true;
                if (Program.ScreenshotInverse) vm.IsInverseMode = true;
                if (Program.ScreenshotInput is { } inputText) vm.ExpressionInput = inputText;
                if (Program.ScreenshotBase is { } b)
                    vm.CurrentBaseMode = b.ToUpperInvariant() switch
                    {
                        "HEX" => BaseMode.Hexadecimal,
                        "OCT" => BaseMode.Octal,
                        "BIN" => BaseMode.Binary,
                        _ => BaseMode.Decimal
                    };
                if (Program.ScreenshotType is { } typed)
                    foreach (var ch in typed) vm.DigitCommand.Execute(ch.ToString());

                if (Program.ScreenshotExpr is { } expression)
                {
                    vm.ExpressionInput = expression;
                    vm.EvaluateExpressionCommand.Execute(null);
                }

                if (Program.ScreenshotSeq is { } seq)
                {
                    foreach (var tok in seq.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        switch (tok)
                        {
                            case "+": vm.AddCommand.Execute(null); break;
                            case "-": vm.SubtractCommand.Execute(null); break;
                            case "*": vm.MultiplyCommand.Execute(null); break;
                            case "/": vm.DivideCommand.Execute(null); break;
                            case "=": vm.EqualsCommand.Execute(null); break;
                            case "C": vm.ClearCommand.Execute(null); break;
                            case "BS": vm.BackspaceCommand.Execute(null); break;
                            case "±": case "NEG": vm.NegateCommand.Execute(null); break;
                            case ".": vm.DecimalCommand.Execute(null); break;
                            case "AND": vm.BitwiseAndCommand.Execute(null); break;
                            case "OR": vm.BitwiseOrCommand.Execute(null); break;
                            case "XOR": vm.BitwiseXorCommand.Execute(null); break;
                            case "NOT": vm.BitwiseNotCommand.Execute(null); break;
                            case "<<": case "SHL": vm.LeftShiftCommand.Execute(null); break;
                            case ">>": case "SHR": vm.RightShiftCommand.Execute(null); break;
                            case "MS": vm.MemoryStoreCommand.Execute(null); break;
                            case "MR": vm.MemoryRecallCommand.Execute(null); break;
                            case "M+": vm.MemoryAddCommand.Execute(null); break;
                            case "M-": vm.MemorySubtractCommand.Execute(null); break;
                            case "MC": vm.MemoryClearCommand.Execute(null); break;
                            default:
                                foreach (var ch in tok) vm.DigitCommand.Execute(ch.ToString());
                                break;
                        }
                    }
                }

                if (Program.ScreenshotDump)
                {
                    Console.WriteLine($"DISPLAY={vm.DisplayText}");
                    Console.WriteLine($"EXPR={vm.Expression}");
                    Console.Out.Flush();
                    Environment.Exit(0);
                }
            }
            desktop.MainWindow = window;

            if (Program.ScreenshotPath is { } shotPath)
            {
                window.Opened += async (_, _) =>
                {
                    // Move the window away from the real cursor so no key renders in a hover state.
                    try { window.Position = new PixelPoint(-4000, -4000); } catch { }
                    if (Program.ScreenshotSwitchMode is { } switchMode &&
                        window.DataContext is CalculatorViewModel switchVm &&
                        Enum.TryParse<CalculatorMode>(switchMode, true, out var switchTo))
                    {
                        // Runtime mode switch: the window must resize to the destination mode here.
                        switchVm.SelectedMode = switchTo;
                        await Task.Delay(400);
                    }
                    await Task.Delay(800);
                    var w = Math.Max(1, (int)window.Bounds.Width);
                    var h = Math.Max(1, (int)window.Bounds.Height);
                    using var bitmap = new RenderTargetBitmap(new PixelSize(w, h), new Vector(96, 96));
                    bitmap.Render(window);
                    bitmap.Save(shotPath, PngBitmapEncoderOptions.Default);
                    desktop.Shutdown();
                };
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
