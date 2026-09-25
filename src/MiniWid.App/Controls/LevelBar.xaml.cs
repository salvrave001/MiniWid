using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ShapePath = Microsoft.UI.Xaml.Shapes.Path;
using ShapeRect = Microsoft.UI.Xaml.Shapes.Rectangle;
using Windows.Foundation;
using Windows.UI;

namespace MiniWid.App.Controls;

public sealed partial class LevelBar : UserControl
{
    public static readonly DependencyProperty PercentProperty =
        DependencyProperty.Register(
            nameof(Percent),
            typeof(int),
            typeof(LevelBar),
            new PropertyMetadata(0, OnVisualChanged));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(LevelBar),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty TrackBrushProperty =
        DependencyProperty.Register(
            nameof(TrackBrush),
            typeof(Brush),
            typeof(LevelBar),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty BarStyleProperty =
        DependencyProperty.Register(
            nameof(BarStyle),
            typeof(string),
            typeof(LevelBar),
            new PropertyMetadata("slash", OnVisualChanged));

    public static readonly DependencyProperty BarCornerProperty =
        DependencyProperty.Register(
            nameof(BarCorner),
            typeof(CornerRadius),
            typeof(LevelBar),
            new PropertyMetadata(default(CornerRadius), OnVisualChanged));

    public LevelBar()
    {
        InitializeComponent();
        SizeChanged += (_, _) => Paint();
        Loaded += (_, _) => Paint();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width) ? availableSize.Width : 0;
        return new Size(Math.Max(0, width), 16);
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

    public Brush? TrackBrush
    {
        get => (Brush?)GetValue(TrackBrushProperty);
        set => SetValue(TrackBrushProperty, value);
    }

    public string BarStyle
    {
        get => (string)GetValue(BarStyleProperty);
        set => SetValue(BarStyleProperty, value);
    }

    public CornerRadius BarCorner
    {
        get => (CornerRadius)GetValue(BarCornerProperty);
        set => SetValue(BarCornerProperty, value);
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LevelBar bar)
        {
            bar.Paint();
        }
    }

    private void Paint()
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width < 8 || height < 4)
        {
            return;
        }

        Surface.Width = width;
        Surface.Height = height;
        Surface.Children.Clear();

        var ratio = Math.Clamp(Percent / 100.0, 0, 1);
        var color = ColorOf(Fill) ?? Color.FromArgb(255, 224, 190, 98);
        var track = ColorOf(TrackBrush) ?? Color.FromArgb(255, 28, 28, 28);
        if (BarStyle == "pill")
        {
            PaintPill(width, height, ratio, color, track);
            return;
        }

        if (BarStyle == "segments")
        {
            PaintSegments(width, height, ratio, color);
            return;
        }

        var slant = Math.Min(height * 0.92, width * 0.22);
        var inner = Math.Max(width - slant, 1);

        AddPath(Parallelogram(slant, width, 0, width - slant, height), new SolidColorBrush(Darken(track, 0.35)));

        if (ratio > 0.004)
        {
            var top = slant + (inner * ratio);
            var bottom = inner * ratio;
            AddPath(Parallelogram(slant, top, 0, bottom, height), FillBrush(color));

            var gap = 1.35;
            var step = 9.5;
            for (var mark = step; mark < inner - 1; mark += step)
            {
                if (mark + gap >= bottom)
                {
                    break;
                }

                AddPath(
                    Parallelogram(slant + mark, slant + mark + gap, mark, mark + gap, height),
                    new SolidColorBrush(Color.FromArgb(210, 8, 8, 8)));
            }
        }

        var frame = new ShapePath
        {
            Data = Parallelogram(slant, width, 0, width - slant, height),
            Stroke = new SolidColorBrush(Color.FromArgb(255, 58, 58, 58)),
            StrokeThickness = 1.15,
            StrokeLineJoin = PenLineJoin.Miter
        };
        Surface.Children.Add(frame);
    }

    private void PaintSegments(double width, double height, double ratio, Color color)
    {
        const double segment = 3.2;
        const double gap = 2;
        var count = Math.Max(1, (int)Math.Floor((width + gap) / (segment + gap)));
        var filled = (int)Math.Round(count * ratio);
        var total = (count * segment) + ((count - 1) * gap);
        var x = Math.Max(0, (width - total) / 2);
        var empty = Color.FromArgb(255, 46, 46, 46);

        for (var i = 0; i < count; i++)
        {
            var tick = new ShapeRect
            {
                Width = segment,
                Height = height,
                RadiusX = 0.6,
                RadiusY = 0.6,
                Fill = new SolidColorBrush(i < filled ? color : empty)
            };
            Canvas.SetLeft(tick, x);
            Surface.Children.Add(tick);
            x += segment + gap;
        }
    }

    private void PaintPill(double width, double height, double ratio, Color color, Color track)
    {
        var radius = height / 2;
        Surface.Children.Add(new ShapeRect
        {
            Width = width,
            Height = height,
            RadiusX = radius,
            RadiusY = radius,
            Fill = new SolidColorBrush(Color.FromArgb(90, track.R, track.G, track.B))
        });

        if (ratio <= 0.004)
        {
            return;
        }

        var fillWidth = Math.Clamp(width * ratio, height, width);
        Surface.Children.Add(new ShapeRect
        {
            Width = fillWidth,
            Height = height,
            RadiusX = radius,
            RadiusY = radius,
            Fill = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0.5),
                EndPoint = new Point(1, 0.5),
                GradientStops =
                {
                    new GradientStop { Color = color, Offset = 0 },
                    new GradientStop { Color = Lighten(color, 0.45), Offset = 1 }
                }
            }
        });
    }

    private void AddPath(Geometry data, Brush fill)
    {
        Surface.Children.Add(new ShapePath
        {
            Data = data,
            Fill = fill
        });
    }

    private static PathGeometry Parallelogram(double topLeft, double topRight, double bottomLeft, double bottomRight, double height)
    {
        var figure = new PathFigure
        {
            StartPoint = new Point(topLeft, 0.6),
            IsClosed = true
        };
        figure.Segments.Add(new LineSegment { Point = new Point(topRight, 0.6) });
        figure.Segments.Add(new LineSegment { Point = new Point(bottomRight, height - 0.6) });
        figure.Segments.Add(new LineSegment { Point = new Point(bottomLeft, height - 0.6) });
        return new PathGeometry { Figures = { figure } };
    }

    private static LinearGradientBrush FillBrush(Color color) => new()
    {
        StartPoint = new Point(0, 0),
        EndPoint = new Point(0, 1),
        GradientStops =
        {
            new GradientStop { Color = Lighten(color, 0.42), Offset = 0 },
            new GradientStop { Color = color, Offset = 0.42 },
            new GradientStop { Color = Darken(color, 0.22), Offset = 1 }
        }
    };

    private static Color? ColorOf(Brush? brush) =>
        brush is SolidColorBrush solid ? solid.Color : null;

    private static Color Lighten(Color color, double amount)
    {
        static byte Mix(byte channel, double amount) =>
            (byte)Math.Clamp(channel + ((255 - channel) * amount), 0, 255);

        return Color.FromArgb(color.A, Mix(color.R, amount), Mix(color.G, amount), Mix(color.B, amount));
    }

    private static Color Darken(Color color, double amount)
    {
        static byte Mix(byte channel, double amount) =>
            (byte)Math.Clamp(channel * (1 - amount), 0, 255);

        return Color.FromArgb(color.A, Mix(color.R, amount), Mix(color.G, amount), Mix(color.B, amount));
    }
}
