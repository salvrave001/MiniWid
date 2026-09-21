using System.Globalization;
using MrtResourceLoader = Microsoft.Windows.ApplicationModel.Resources.ResourceLoader;

namespace MiniWid.App.Services;

internal sealed class Strings
{
    private readonly MrtResourceLoader? _loader;
    private readonly Dictionary<string, string> _fallback;

    public Strings()
    {
        _fallback = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru"
            ? Russian
            : English;

        try
        {
            _loader = new MrtResourceLoader();
        }
        catch
        {
            _loader = null;
        }
    }

    public string this[string key]
    {
        get
        {
            try
            {
                var value = _loader?.GetString(key);
                if (!string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }
            catch
            {
                // Use compiled fallbacks when PRI lookup is unavailable.
            }

            return _fallback.TryGetValue(key, out var text) ? text : key;
        }
    }

    private static readonly Dictionary<string, string> English = new()
    {
        ["AppTitle/Text"] = "Battery Status",
        ["Connected"] = "Connected",
        ["Disconnected"] = "Disconnected",
        ["Charging"] = "Charging",
        ["Sleeping"] = "Sleep mode",
        ["ThisPc"] = "This PC",
        ["EmptyMessage"] = "No battery devices found. Windows only reports accessories that already show a charge in Settings.",
        ["ThemeHeader"] = "Theme",
        ["ThemeRog"] = "ASUS ROG",
        ["ThemeRog20"] = "ROG 20th Anniversary",
        ["ThemeCyberpunk"] = "Cyberpunk 2077",
        ["ThemeWindowsDark"] = "Windows dark",
        ["ThemeWindowsLight"] = "Windows light",
        ["AlwaysOnTop"] = "Always on top",
        ["ShowThisPc"] = "Show This PC",
        ["StartWithWindows"] = "Start with Windows",
        ["RefreshNow"] = "Refresh now",
        ["PollHeader"] = "Refresh interval",
        ["Poll15"] = "Every 15 seconds",
        ["Poll30"] = "Every 30 seconds",
        ["Poll60"] = "Every 60 seconds",
        ["ShowWidget"] = "Show widget",
        ["HideWidget"] = "Hide widget",
        ["Exit"] = "Exit",
        ["TrayTooltip"] = "MiniWid — battery status"
    };

    private static readonly Dictionary<string, string> Russian = new()
    {
        ["AppTitle/Text"] = "Статус батареи",
        ["Connected"] = "Подключено",
        ["Disconnected"] = "Отключено",
        ["Charging"] = "Зарядка",
        ["Sleeping"] = "Спящий режим",
        ["ThisPc"] = "Этот компьютер",
        ["EmptyMessage"] = "Устройства с зарядом не найдены. Виджет показывает только то, что Windows уже знает в Параметрах.",
        ["ThemeHeader"] = "Тема",
        ["ThemeRog"] = "ASUS ROG",
        ["ThemeRog20"] = "ROG 20th Anniversary",
        ["ThemeCyberpunk"] = "Cyberpunk 2077",
        ["ThemeWindowsDark"] = "Windows, тёмная",
        ["ThemeWindowsLight"] = "Windows, светлая",
        ["AlwaysOnTop"] = "Поверх окон",
        ["ShowThisPc"] = "Показывать этот ПК",
        ["StartWithWindows"] = "Запускать с Windows",
        ["RefreshNow"] = "Обновить",
        ["PollHeader"] = "Интервал обновления",
        ["Poll15"] = "Каждые 15 секунд",
        ["Poll30"] = "Каждые 30 секунд",
        ["Poll60"] = "Каждые 60 секунд",
        ["ShowWidget"] = "Показать виджет",
        ["HideWidget"] = "Скрыть виджет",
        ["Exit"] = "Выход",
        ["TrayTooltip"] = "MiniWid — статус батареи"
    };
}
