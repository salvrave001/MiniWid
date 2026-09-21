using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace MiniWid.App.Controls;

public sealed partial class BatteryGlyph : UserControl
{
    public static readonly DependencyProperty PercentProperty =
        DependencyProperty.Register(
            nameof(Percent),
            typeof(int),
            typeof(BatteryGlyph),
            new PropertyMetadata(0, OnVisualChanged));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(BatteryGlyph),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty StrokeProperty =
        DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(BatteryGlyph),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty EmptyProperty =
        DependencyProperty.Register(
            nameof(Empty),
            typeof(Brush),
            typeof(BatteryGlyph),
            new PropertyMetadata(null, OnVisualChanged));

    public BatteryGlyph()
    {
        InitializeComponent();
        Loaded += (_, _) => Paint();
    }

    public int Percent
    {
        get => (int)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush? Stroke
    {
        get => (Brush?)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public Brush? Empty
    {
        get => (Brush?)GetValue(EmptyProperty);
        set => SetValue(EmptyProperty, value);
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BatteryGlyph glyph && glyph.IsLoaded)
        {
            glyph.Paint();
        }
    }

    private void OnWellSizeChanged(object sender, SizeChangedEventArgs e) => Paint();

    private void Paint()
    {
        var color = SolidColor(Fill) ?? SolidColor(Stroke) ?? Color.FromArgb(255, 224, 190, 98);
        var stroke = new SolidColorBrush(color);
        Body.BorderBrush = stroke;
        Nub.Background = stroke;
        Well.Background = new SolidColorBrush(Color.FromArgb(36, color.R, color.G, color.B));

        var ratio = Math.Clamp(Percent / 100.0, 0, 1);
        var inner = Well.ActualWidth;
        if (inner <= 0)
        {
            return;
        }

        FillBar.Width = ratio <= 0 ? 0 : Math.Min(inner, Math.Max(2.2, inner * ratio));
        FillBar.Background = MakeFill(color);
        FillBar.CornerRadius = ratio >= 0.96
            ? new CornerRadius(1.45)
            : new CornerRadius(1.45, 0.55, 0.55, 1.45);
        FillBar.Opacity = ratio <= 0 ? 0 : 1;
    }

    private static Color? SolidColor(Brush? brush) =>
        brush is SolidColorBrush solid ? solid.Color : null;

    private static Brush MakeFill(Color color) => new LinearGradientBrush
    {
        StartPoint = new Windows.Foundation.Point(0.5, 0),
        EndPoint = new Windows.Foundation.Point(0.5, 1),
        GradientStops =
        {
            new GradientStop { Color = Mix(color, Color.FromArgb(255, 255, 255, 255), 0.16), Offset = 0 },
            new GradientStop { Color = color, Offset = 0.55 },
            new GradientStop { Color = Mix(color, Color.FromArgb(255, 0, 0, 0), 0.08), Offset = 1 }
        }
    };

    private static Color Mix(Color a, Color b, double amount)
    {
        amount = Math.Clamp(amount, 0, 1);
        return Color.FromArgb(
            a.A,
            (byte)(a.R + (b.R - a.R) * amount),
            (byte)(a.G + (b.G - a.G) * amount),
            (byte)(a.B + (b.B - a.B) * amount));
    }
}
