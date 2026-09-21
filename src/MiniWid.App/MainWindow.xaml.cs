using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using MiniWid.App.Services;
using MiniWid.App.ViewModels;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

namespace MiniWid.App;

public sealed partial class MainWindow : Window
{
    private const int WindowWidth = 408;
    private const nuint SubclassId = 1;

    private readonly SettingsStore _settingsStore = new();
    private readonly WidgetViewModel _viewModel = new();
    private readonly Strings _resources = new();
    private readonly DispatcherTimer _timer = new();

    private AppSettings _settings;
    private WidgetTheme _theme = WidgetTheme.From(WidgetTheme.Rog20);
    private TrayIconService? _tray;
    private WindowSubclass.SubclassProc? _subclassProc;
    private IntPtr _hwnd;
    private bool _allowClose;
    private bool _subclassInstalled;

    public MainWindow()
    {
        InitializeComponent();
        _settings = _settingsStore.Load();
        _settings.Theme = WidgetTheme.Normalize(_settings.Theme);
        _settings.StartWithWindows = StartupService.IsEnabled();

        DeviceList.ItemsSource = _viewModel.Devices;
        LocalizeMenus();
        ApplyTheme();
        ConfigureWindow();
        ApplyAlwaysOnTop();
        RestorePosition();

        _timer.Tick += async (_, _) => await RefreshDevicesAsync();
        ResetTimer();

        Closed += MainWindow_Closed;
        AppWindow.Closing += AppWindow_Closing;
        Root.Loaded += Root_Loaded;
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        InstallTray();
        await RefreshDevicesAsync();
    }

    private void ConfigureWindow()
    {
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(HeaderBar);
        AppWindow.IsShownInSwitchers = false;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
        AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        try
        {
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
        }
        catch (NotImplementedException)
        {
            // Older Windows App SDK hosts may not collapse the caption height.
        }

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.SetBorderAndTitleBar(true, false);
        }

        ResizeToContent(_viewModel.Devices.Count);
    }

    private void RestorePosition()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var work = display.WorkArea;
        var x = _settings.WindowX;
        var y = _settings.WindowY;

        if (x == int.MinValue || y == int.MinValue)
        {
            x = work.X + work.Width - WindowWidth - 28;
            y = work.Y + 28;
        }

        AppWindow.Move(new PointInt32(x, y));
    }

    private void SavePosition()
    {
        var pos = AppWindow.Position;
        _settings.WindowX = pos.X;
        _settings.WindowY = pos.Y;
        _settingsStore.Save(_settings);
    }

    private ResourceDictionary? _skinDark;
    private ResourceDictionary? _skinLight;

    private void ApplyTheme()
    {
        _theme = WidgetTheme.From(_settings.Theme);

        _skinDark ??= new ResourceDictionary();
        _skinLight ??= new ResourceDictionary();
        FillSkin(_skinDark, _theme);
        FillSkin(_skinLight, _theme);

        Root.Resources.ThemeDictionaries["Dark"] = _skinDark;
        Root.Resources.ThemeDictionaries["Light"] = _skinLight;

        // Drop leftover local overrides from the previous approach so they
        // cannot sit on top of the new ThemeDictionaries.
        foreach (var key in SkinKeys)
        {
            if (Root.Resources.ContainsKey(key))
            {
                Root.Resources.Remove(key);
            }
        }

        var opposite = _theme.ElementTheme == ElementTheme.Light ? ElementTheme.Dark : ElementTheme.Light;
        Root.RequestedTheme = opposite;
        Root.RequestedTheme = _theme.ElementTheme;

        ApplyChrome(_theme);
        RebuildDeviceList();
        ResizeToContent(_viewModel.Devices.Count);
    }

    private static readonly string[] SkinKeys =
    [
        "MiniWidCardBrush", "MiniWidPrimaryBrush", "MiniWidSecondaryBrush", "MiniWidRowBrush",
        "MiniWidRowBorderBrush", "MiniWidHeaderIconBrush", "MiniWidMenuBrush", "MiniWidAccentBrush",
        "MiniWidAccentStrongBrush", "MiniWidBorderBrush", "MiniWidSlashBrush", "MiniWidRowEdgeBrush",
        "MiniWidUiFont", "MiniWidRowCornerRadius", "MiniWidBatteryCornerRadius",
        "MiniWidSlashOpacity", "MiniWidRowEdgeOpacity", "MiniWidPhotoGlowBrush"
    ];

    private static void FillSkin(ResourceDictionary dict, WidgetTheme theme)
    {
        SetBrush(dict, "MiniWidCardBrush", theme.Card);
        SetBrush(dict, "MiniWidPrimaryBrush", theme.Primary);
        SetBrush(dict, "MiniWidSecondaryBrush", theme.Secondary);
        SetBrush(dict, "MiniWidRowBrush", theme.Row);
        SetBrush(dict, "MiniWidRowBorderBrush", theme.RowBorder);
        SetBrush(dict, "MiniWidHeaderIconBrush", theme.Accent);
        SetBrush(dict, "MiniWidMenuBrush", theme.Menu);
        SetBrush(dict, "MiniWidAccentBrush", theme.Accent);
        SetBrush(dict, "MiniWidAccentStrongBrush", theme.AccentStrong);
        SetBrush(dict, "MiniWidBorderBrush", theme.Border);
        SetBrush(dict, "MiniWidSlashBrush", theme.Slash);
        SetBrush(dict, "MiniWidRowEdgeBrush", theme.RowEdge);
        dict["MiniWidUiFont"] = theme.Font;
        dict["MiniWidRowCornerRadius"] = new CornerRadius(theme.RowCorner);
        dict["MiniWidBatteryCornerRadius"] = new CornerRadius(theme.BatteryCorner);
        dict["MiniWidSlashOpacity"] = theme.SlashOpacity;
        dict["MiniWidRowEdgeOpacity"] = theme.RowEdgeOpacity;
        SetPhotoGlow(dict, theme.Accent);
    }

    private static void SetBrush(ResourceDictionary dict, string key, string hex)
    {
        var color = WidgetTheme.Parse(hex);
        if (dict.ContainsKey(key) && dict[key] is SolidColorBrush existing)
        {
            existing.Color = color;
            return;
        }

        dict[key] = new SolidColorBrush(color);
    }

    private static void SetPhotoGlow(ResourceDictionary dict, string hex)
    {
        var color = WidgetTheme.Parse(hex);
        if (dict.ContainsKey("MiniWidPhotoGlowBrush") && dict["MiniWidPhotoGlowBrush"] is LinearGradientBrush existing
            && existing.GradientStops.Count >= 3)
        {
            existing.GradientStops[0].Color = Color.FromArgb(220, color.R, color.G, color.B);
            existing.GradientStops[1].Color = Color.FromArgb(110, color.R, color.G, color.B);
            existing.GradientStops[2].Color = Color.FromArgb(0, color.R, color.G, color.B);
            return;
        }

        dict["MiniWidPhotoGlowBrush"] = new LinearGradientBrush
        {
            StartPoint = new Windows.Foundation.Point(0.5, 1),
            EndPoint = new Windows.Foundation.Point(0.5, 0),
            GradientStops =
            {
                new GradientStop { Color = Color.FromArgb(220, color.R, color.G, color.B), Offset = 0 },
                new GradientStop { Color = Color.FromArgb(110, color.R, color.G, color.B), Offset = 0.32 },
                new GradientStop { Color = Color.FromArgb(0, color.R, color.G, color.B), Offset = 1 }
            }
        };
    }

    private void ApplyChrome(WidgetTheme theme)
    {
        var card = BrushFrom(theme.ElementTheme, "MiniWidCardBrush");
        var primary = BrushFrom(theme.ElementTheme, "MiniWidPrimaryBrush");
        var secondary = BrushFrom(theme.ElementTheme, "MiniWidSecondaryBrush");
        var accent = BrushFrom(theme.ElementTheme, "MiniWidAccentBrush");
        var accentStrong = BrushFrom(theme.ElementTheme, "MiniWidAccentStrongBrush");
        var menu = BrushFrom(theme.ElementTheme, "MiniWidMenuBrush");
        var border = BrushFrom(theme.ElementTheme, "MiniWidBorderBrush");
        var slash = BrushFrom(theme.ElementTheme, "MiniWidSlashBrush");

        Root.Background = card;
        CardFrame.BorderBrush = border;
        TopSlash.Fill = slash;
        BottomSlash.Fill = slash;
        BrandMark.Fill = slash;
        HeaderRule.Fill = slash;
        TitleText.Foreground = theme.Id == WidgetTheme.Cyberpunk ? accent : primary;
        TitleText.FontFamily = theme.Font;
        TitleText.FontSize = theme.TitleSize;
        TitleText.CharacterSpacing = theme.TitleSpacing;
        EditionMark.Foreground = accentStrong;
        EditionMark.FontFamily = theme.Font;
        EditionDivider.Background = border;
        EmptyText.Foreground = secondary;
        EmptyText.FontFamily = theme.Font;
        MoreIcon.Foreground = menu;
        CyberIndexBox.BorderBrush = accent;
        CyberIndexText.Foreground = accent;
        CyberIndexText.FontFamily = theme.Font;
        CyberBadge.FontFamily = theme.Font;
        CyberBadge.Foreground = secondary;

        var cyber = theme.Id == WidgetTheme.Cyberpunk;
        var slashVis = theme.ShowSlashes ? Visibility.Visible : Visibility.Collapsed;
        TopSlash.Visibility = slashVis;
        BottomSlash.Visibility = slashVis;
        HeaderRule.Visibility = cyber ? Visibility.Collapsed : slashVis;
        BrandMark.Visibility = theme.ShowBrandMark ? Visibility.Visible : Visibility.Collapsed;
        CyberHud.Visibility = cyber ? Visibility.Visible : Visibility.Collapsed;
        CyberIndexBox.Visibility = cyber ? Visibility.Visible : Visibility.Collapsed;
        CyberBadge.Visibility = cyber ? Visibility.Visible : Visibility.Collapsed;
        var hasBadge = !string.IsNullOrWhiteSpace(theme.HeaderBadge) && !cyber;
        EditionMark.Text = hasBadge ? theme.HeaderBadge : "20";
        EditionMark.Visibility = hasBadge ? Visibility.Visible : Visibility.Collapsed;
        EditionMark.CharacterSpacing = hasBadge ? 80 : 40;
        EditionDivider.Visibility = Visibility.Collapsed;
        TitleText.Visibility = cyber ? Visibility.Visible : Visibility.Collapsed;
        Rog20Logo.Visibility = theme.ShowEdition20 ? Visibility.Visible : Visibility.Collapsed;
        if (theme.ShowEdition20)
        {
            EnsureRog20Logo();
        }

        InnerShell.Margin = theme.ContentMargin;
        ListHost.Padding = cyber
            ? new Thickness(16, 6, 16, 14)
            : theme.ShowSlashes
                ? new Thickness(14, 10, 26, 20)
                : new Thickness(4, 4, 10, 10);
        HeaderBar.Padding = cyber
            ? new Thickness(16, 12, 10, 8)
            : theme.ShowSlashes
                ? new Thickness(16, 10, 10, 10)
                : new Thickness(4, 8, 4, 8);
        MoreButton.CornerRadius = new CornerRadius(theme.RowCorner > 0 ? 4 : 0);

        var title = _resources["AppTitle/Text"];
        TitleText.Text = theme.TitleAllCaps ? title.ToUpperInvariant() : title;
    }

    private void EnsureRog20Logo()
    {
        if (Rog20Logo.Source is not null)
        {
            return;
        }

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Rog20Logo.png");
            using var file = File.OpenRead(path);
            var image = new BitmapImage();
            image.SetSource(file.AsRandomAccessStream());
            Rog20Logo.Source = image;
        }
        catch
        {
            try
            {
                Rog20Logo.Source = new BitmapImage(new Uri("ms-appx:///Assets/Rog20Logo.png"));
            }
            catch
            {
                Rog20Logo.Visibility = Visibility.Collapsed;
            }
        }
    }

    private Brush BrushFrom(ElementTheme elementTheme, string key)
    {
        var dict = elementTheme == ElementTheme.Light ? _skinLight : _skinDark;
        return (Brush)dict![key];
    }

    private void RebuildDeviceList()
    {
        DeviceList.ItemsSource = null;
        DeviceList.ItemsSource = _viewModel.Devices;
    }

    private void ApplyAlwaysOnTop()
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = _settings.AlwaysOnTop;
        }
    }

    private void ResetTimer()
    {
        var seconds = Math.Clamp(_settings.PollIntervalSeconds, 15, 120);
        _timer.Interval = TimeSpan.FromSeconds(seconds);
        _timer.Stop();
        _timer.Start();
    }

    private async Task RefreshDevicesAsync()
    {
        await _viewModel.RefreshAsync(
            _settings.ShowThisPc,
            _resources["Connected"],
            _resources["Disconnected"],
            _resources["Charging"],
            _resources["Sleeping"],
            _resources["ThisPc"],
            _resources["EmptyMessage"],
            _theme.FillHealthy,
            _theme.FillWarn,
            _theme.FillLow);

        EmptyText.Text = _viewModel.StatusMessage;
        EmptyText.Visibility = _viewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        DeviceList.Visibility = _viewModel.IsEmpty ? Visibility.Collapsed : Visibility.Visible;
        ResizeToContent(_viewModel.Devices.Count);
        _tray?.UpdateTooltip(BuildTrayTooltip());
    }

    private void ResizeToContent(int count)
    {
        var height = _theme.WindowHeight(count, _viewModel.IsEmpty);
        AppWindow.Resize(new SizeInt32(WindowWidth, height));
    }

    private void LocalizeMenus()
    {
        var title = _resources["AppTitle/Text"];
        TitleText.Text = _theme.TitleAllCaps ? title.ToUpperInvariant() : title;
        ThemeMenu.Text = _resources["ThemeHeader"];
        ThemeRogItem.Text = _resources["ThemeRog"];
        ThemeRog20Item.Text = _resources["ThemeRog20"];
        ThemeCyberpunkItem.Text = _resources["ThemeCyberpunk"];
        ThemeWindowsDarkItem.Text = _resources["ThemeWindowsDark"];
        ThemeWindowsLightItem.Text = _resources["ThemeWindowsLight"];
        AlwaysOnTopItem.Text = _resources["AlwaysOnTop"];
        ShowThisPcItem.Text = _resources["ShowThisPc"];
        StartWithWindowsItem.Text = _resources["StartWithWindows"];
        PollMenu.Text = _resources["PollHeader"];
        Poll15Item.Text = _resources["Poll15"];
        Poll30Item.Text = _resources["Poll30"];
        Poll60Item.Text = _resources["Poll60"];
        RefreshItem.Text = _resources["RefreshNow"];
        ExitItem.Text = _resources["Exit"];
        TrayShowItem.Text = _resources["ShowWidget"];
        TrayHideItem.Text = _resources["HideWidget"];
        TrayExitItem.Text = _resources["Exit"];
    }

    private void SettingsFlyout_Opening(object sender, object e)
    {
        ThemeRogItem.IsChecked = _settings.Theme == WidgetTheme.Rog;
        ThemeRog20Item.IsChecked = _settings.Theme == WidgetTheme.Rog20;
        ThemeCyberpunkItem.IsChecked = _settings.Theme == WidgetTheme.Cyberpunk;
        ThemeWindowsDarkItem.IsChecked = _settings.Theme == WidgetTheme.WindowsDark;
        ThemeWindowsLightItem.IsChecked = _settings.Theme == WidgetTheme.WindowsLight;
        AlwaysOnTopItem.IsChecked = _settings.AlwaysOnTop;
        ShowThisPcItem.IsChecked = _settings.ShowThisPc;
        StartWithWindowsItem.IsChecked = _settings.StartWithWindows;
        Poll15Item.IsChecked = _settings.PollIntervalSeconds == 15;
        Poll30Item.IsChecked = _settings.PollIntervalSeconds == 30;
        Poll60Item.IsChecked = _settings.PollIntervalSeconds == 60;
    }

    private async void Theme_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item && item.Tag is string theme)
        {
            _settings.Theme = WidgetTheme.Normalize(theme);
            _settingsStore.Save(_settings);
            ApplyTheme();
            await RefreshDevicesAsync();
        }
    }

    private void AlwaysOnTop_Click(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = AlwaysOnTopItem.IsChecked;
        _settingsStore.Save(_settings);
        ApplyAlwaysOnTop();
    }

    private async void ShowThisPc_Click(object sender, RoutedEventArgs e)
    {
        _settings.ShowThisPc = ShowThisPcItem.IsChecked;
        _settingsStore.Save(_settings);
        await RefreshDevicesAsync();
    }

    private void StartWithWindows_Click(object sender, RoutedEventArgs e)
    {
        _settings.StartWithWindows = StartWithWindowsItem.IsChecked;
        StartupService.SetEnabled(_settings.StartWithWindows);
        _settingsStore.Save(_settings);
    }

    private void Poll_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item && int.TryParse(item.Tag as string, out var seconds))
        {
            _settings.PollIntervalSeconds = seconds;
            _settingsStore.Save(_settings);
            ResetTimer();
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshDevicesAsync();

    private void TrayShow_Click(object sender, RoutedEventArgs e) => ShowWidget();

    private void TrayHide_Click(object sender, RoutedEventArgs e) => AppWindow.Hide();

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _allowClose = true;
        Close();
    }

    private void ShowWidget()
    {
        AppWindow.Show();
        Activate();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_allowClose)
        {
            return;
        }

        args.Cancel = true;
        SavePosition();
        AppWindow.Hide();
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        SavePosition();
        _timer.Stop();
        UninstallTray();
    }

    private void InstallTray()
    {
        _hwnd = WindowNative.GetWindowHandle(this);
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        _tray = new TrayIconService(_hwnd, iconPath, BuildTrayTooltip());
        _subclassProc = SubclassWndProc;
        _subclassInstalled = WindowSubclass.SetWindowSubclass(_hwnd, _subclassProc, SubclassId, IntPtr.Zero);
    }

    private void UninstallTray()
    {
        if (_subclassInstalled && _subclassProc is not null)
        {
            WindowSubclass.RemoveWindowSubclass(_hwnd, _subclassProc, SubclassId);
            _subclassInstalled = false;
        }

        _tray?.Dispose();
        _tray = null;
    }

    private IntPtr SubclassWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, nuint uIdSubclass, IntPtr dwRefData)
    {
        if (msg == TrayIconService.WmTrayIcon)
        {
            var mouse = lParam.ToInt32() & 0xFFFF;
            var point = TrayIconService.PointFromCallback(wParam);
            if (point.X == 0 && point.Y == 0)
            {
                var cursor = TrayIconService.CursorPosition();
                point = (cursor.X, cursor.Y);
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                if (mouse is TrayIconService.WmLButtonUp or TrayIconService.WmLButtonDblClk)
                {
                    ShowWidget();
                }
                else if (mouse is TrayIconService.WmRButtonUp or TrayIconService.WmContextMenu)
                {
                    ShowTrayMenu(point.X, point.Y);
                }
            });
        }

        return WindowSubclass.DefSubclassProc(hWnd, msg, wParam, lParam);
    }

    private void ShowTrayMenu(int screenX, int screenY)
    {
        var scale = Root.XamlRoot?.RasterizationScale ?? 1.0;
        var origin = AppWindow.Position;
        var x = (screenX - origin.X) / scale;
        var y = (screenY - origin.Y) / scale;
        TrayMenu.ShowAt(Root, new Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowOptions
        {
            Placement = Microsoft.UI.Xaml.Controls.Primitives.FlyoutPlacementMode.TopEdgeAlignedLeft,
            Position = new Windows.Foundation.Point(x, y),
            ShowMode = Microsoft.UI.Xaml.Controls.Primitives.FlyoutShowMode.Standard
        });
    }

    private string BuildTrayTooltip()
    {
        if (_viewModel.Devices.Count == 0)
        {
            return _resources["TrayTooltip"];
        }

        var lines = _viewModel.Devices.Take(4).Select(d => $"{d.Name} {d.PercentText}");
        return string.Join(Environment.NewLine, lines);
    }
}
