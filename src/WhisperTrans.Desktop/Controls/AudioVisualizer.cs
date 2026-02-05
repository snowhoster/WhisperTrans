using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WhisperTrans.Desktop.Controls;

/// <summary>
/// 音訊視覺化控制項 - 顯示聲音波動狀態
/// </summary>
public class AudioVisualizer : UserControl
{
    private readonly Canvas _canvas;
    private readonly List<Rectangle> _bars = new();
    private readonly int _barCount = 20;
    private float _currentLevel = 0;
    private float _peakLevel = 0;
    private bool _isSpeechDetected = false;

    public static readonly DependencyProperty AudioLevelProperty =
        DependencyProperty.Register(nameof(AudioLevel), typeof(float), typeof(AudioVisualizer),
            new PropertyMetadata(0f, OnAudioLevelChanged));

    public static readonly DependencyProperty PeakLevelProperty =
        DependencyProperty.Register(nameof(PeakLevel), typeof(float), typeof(AudioVisualizer),
            new PropertyMetadata(0f));

    public static readonly DependencyProperty IsSpeechDetectedProperty =
        DependencyProperty.Register(nameof(IsSpeechDetected), typeof(bool), typeof(AudioVisualizer),
            new PropertyMetadata(false, OnSpeechDetectionChanged));

    public float AudioLevel
    {
        get => (float)GetValue(AudioLevelProperty);
        set => SetValue(AudioLevelProperty, value);
    }

    public float PeakLevel
    {
        get => (float)GetValue(PeakLevelProperty);
        set => SetValue(PeakLevelProperty, value);
    }

    public bool IsSpeechDetected
    {
        get => (bool)GetValue(IsSpeechDetectedProperty);
        set => SetValue(IsSpeechDetectedProperty, value);
    }

    public AudioVisualizer()
    {
        _canvas = new Canvas();
        Content = _canvas;
        
        SizeChanged += OnSizeChanged;
        InitializeBars();
    }

    private void InitializeBars()
    {
        _bars.Clear();
        _canvas.Children.Clear();

        for (int i = 0; i < _barCount; i++)
        {
            var bar = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                Height = 5,
                RadiusX = 2,
                RadiusY = 2
            };
            
            _bars.Add(bar);
            _canvas.Children.Add(bar);
        }
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateBars();
    }

    private static void OnAudioLevelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer._currentLevel = (float)e.NewValue;
            visualizer.UpdateBars();
        }
    }

    private static void OnSpeechDetectionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AudioVisualizer visualizer)
        {
            visualizer._isSpeechDetected = (bool)e.NewValue;
            visualizer.UpdateBarColors();
        }
    }

    private void UpdateBars()
    {
        if (_bars.Count == 0 || ActualWidth <= 0)
            return;

        var barWidth = ActualWidth / _barCount - 2;
        var maxHeight = ActualHeight;

        for (int i = 0; i < _barCount; i++)
        {
            var bar = _bars[i];
            
            // 使用正弦波模式創建動態效果
            var phase = (i / (float)_barCount) * Math.PI * 2;
            var baseHeight = Math.Sin(phase + DateTime.Now.Millisecond / 100.0) * 0.3 + 0.7;
            var heightMultiplier = baseHeight * _currentLevel * 10; // 放大效果
            
            var height = Math.Min(maxHeight * heightMultiplier, maxHeight);
            height = Math.Max(height, 5); // 最小高度

            bar.Width = barWidth;
            bar.Height = height;
            Canvas.SetLeft(bar, i * (barWidth + 2));
            Canvas.SetTop(bar, (maxHeight - height) / 2);
        }

        UpdateBarColors();
    }

    private void UpdateBarColors()
    {
        Color color;
        
        if (_isSpeechDetected)
        {
            // 偵測到語音時使用綠色
            color = Color.FromRgb(76, 175, 80);
        }
        else if (_currentLevel > 0.01f)
        {
            // 有聲音但非語音時使用橙色
            color = Color.FromRgb(255, 152, 0);
        }
        else
        {
            // 靜音時使用灰色
            color = Color.FromRgb(158, 158, 158);
        }

        foreach (var bar in _bars)
        {
            bar.Fill = new SolidColorBrush(color);
        }
    }
}
