using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace MiniWid.App.Controls;

public sealed partial class ChargingMeter : UserControl
{
    public static readonly DependencyProperty PercentProperty =
        DependencyProperty.Register(
            nameof(Percent),
            typeof(int),
            typeof(ChargingMeter),
            new PropertyMetadata(0, OnVisualChanged));

    public static readonly DependencyProperty IsChargingProperty =
        DependencyProperty.Register(
            nameof(IsCharging),
            typeof(bool),
            typeof(ChargingMeter),
            new PropertyMetadata(false, OnVisualChanged));

    public static readonly DependencyProperty FillProperty =
        DependencyProperty.Register(
            nameof(Fill),
            typeof(Brush),
            typeof(ChargingMeter),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty EmptyProperty =
        DependencyProperty.Register(
            nameof(Empty),
            typeof(Brush),
            typeof(ChargingMeter),
            new PropertyMetadata(null, OnVisualChanged));

    public static readonly DependencyProperty AnimatedFillProperty =
        DependencyProperty.Register(
            nameof(AnimatedFill),
            typeof(double),
            typeof(ChargingMeter),
            new PropertyMetadata(0.0, (d, e) =>
            {
                if (d is ChargingMeter meter && e.NewValue is double value)
                {
                    meter.Paint(value);
                }
            }));

    private const int SegmentCount = 10;
    private readonly List<Rectangle> _bars = [];
    private Storyboard? _storyboard;

    public ChargingMeter()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += OnSizeChanged;
    }

    public int Percent
    {
        get => (int)GetValue(PercentProperty);
        set => SetValue(PercentProperty, value);
    }

    public bool IsCharging
    {
        get => (bool)GetValue(IsChargingProperty);
        set => SetValue(IsChargingProperty, value);
    }

    public Brush? Fill
    {
        get => (Brush?)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public Brush? Empty
    {
        get => (Brush?)GetValue(EmptyProperty);
        set => SetValue(EmptyProperty, value);
    }

    public double AnimatedFill
    {
        get => (double)GetValue(AnimatedFillProperty);
        set => SetValue(AnimatedFillProperty, value);
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChargingMeter meter && meter.IsLoaded)
        {
            meter.SyncAnimation();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        EnsureBars();
        SyncAnimation();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        StopAnimation();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RootGrid.Clip = new RectangleGeometry
        {
            Rect = new Windows.Foundation.Rect(0, 0, e.NewSize.Width, e.NewSize.Height)
        };
    }

    private void EnsureBars()
    {
        if (_bars.Count > 0)
        {
            return;
        }

        for (var i = 0; i < SegmentCount; i++)
        {
            var bar = new Rectangle
            {
                Width = 5,
                Height = 16,
                RadiusX = 0.4,
                RadiusY = 0.4,
                RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
                RenderTransform = new SkewTransform { AngleX = -32 }
            };
            _bars.Add(bar);
            BarsHost.Children.Add(bar);
        }
    }

    private void SyncAnimation()
    {
        EnsureBars();
        StopAnimation();

        Bolt.Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
        Bolt.Visibility = IsCharging ? Visibility.Visible : Visibility.Collapsed;

        var target = Math.Clamp(Percent / 100.0, 0, 1);
        if (!IsCharging)
        {
            AnimatedFill = target;
            Paint(target);
            return;
        }

        var anim = new DoubleAnimation
        {
            From = 0,
            To = Math.Max(target, 0.15),
            Duration = new Duration(TimeSpan.FromMilliseconds(1450)),
            RepeatBehavior = RepeatBehavior.Forever,
            EnableDependentAnimation = true,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(anim, this);
        Storyboard.SetTargetProperty(anim, nameof(AnimatedFill));
        _storyboard = new Storyboard();
        _storyboard.Children.Add(anim);
        _storyboard.Begin();
    }

    private void StopAnimation()
    {
        _storyboard?.Stop();
        _storyboard = null;
    }

    private void Paint(double progress)
    {
        if (_bars.Count == 0)
        {
            return;
        }

        var fill = Fill ?? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 224, 190, 98));
        var empty = Empty ?? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 58, 58, 58));
        var lit = progress * SegmentCount;

        for (var i = 0; i < _bars.Count; i++)
        {
            _bars[i].Fill = i < lit ? fill : empty;
        }
    }
}
