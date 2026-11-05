#nullable enable
using System;
using System.IO;
using System.Text.Json;
using Avalonia.Styling;

namespace Calcvik.Services
{
    public enum AppTheme
    {
        Light,
        Dark,
        System
    }

    public sealed class ThemeService
    {
        private readonly string _settingsPath;

        public ThemeService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "Calcvik");
            _settingsPath = Path.Combine(dir, "settings.json");
        }

        public AppTheme Load()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return AppTheme.System;
                var json = File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("theme", out var themeProp))
                {
                    var themeStr = themeProp.GetString();
                    if (Enum.TryParse<AppTheme>(themeStr, true, out var theme))
                        return theme;
                }
            }
            catch
            {
                // swallow any error, fall through
            }
            return AppTheme.System;
        }

        public void Save(AppTheme theme)
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                var obj = new { theme = theme.ToString() };
                var json = JsonSerializer.Serialize(obj);
                File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // swallow any error
            }
        }

        public void Apply(AppTheme theme)
        {
            var app = Avalonia.Application.Current;
            if (app == null) return;
            switch (theme)
            {
                case AppTheme.Light:
                    app.RequestedThemeVariant = ThemeVariant.Light;
                    break;
                case AppTheme.Dark:
                    app.RequestedThemeVariant = ThemeVariant.Dark;
                    break;
                case AppTheme.System:
                    app.RequestedThemeVariant = ThemeVariant.Default;
                    break;
            }
        }
    }
}
