using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace VoiceRecogEvalClient.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AudioChartView : ContentView
    {
        public float SilenceThreshold { get; set; }

        private bool _isUpdating;
        public bool IsUpdating 
        {
            get => _isUpdating;
            set
            {
                _isUpdating = value;
                UpdateAudioLevelChartTimer.Change(value ? UpdateAudioChartTime : Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
        }


        ObservableCollection<ObservablePoint> SilenceThresholdHistory;
        ObservableCollection<ObservablePoint> AudioLevelsHistory;
        Timer UpdateAudioLevelChartTimer;
        const int MaxAudioLevelHistoryLength = 20;
        TimeSpan UpdateAudioChartTime = TimeSpan.FromSeconds(0.15);
        int AudioLevelHistoryIndex = 0;
        public AudioChartView()
        {
            InitializeComponent();


            AudioLevelsHistory = new ObservableCollection<ObservablePoint>();
            SilenceThresholdHistory = new ObservableCollection<ObservablePoint>();
            AudioLevelChart.TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden;
            AudioLevelChart.Series = new ObservableCollection<ISeries>
            {
                new ColumnSeries<ObservablePoint>{ Values = AudioLevelsHistory },
                new LineSeries<ObservablePoint>{ Values = SilenceThresholdHistory, Fill = null, LineSmoothness = 0, GeometrySize = 0 }
            };
            //AudioLevelChart.ZoomingSpeed = 0;
            var xAxis = AudioLevelChart.XAxes.First();
            var yAxis = AudioLevelChart.YAxes.First();
            yAxis.MinLimit = 0;
            yAxis.MaxLimit = 1;
            yAxis.ShowSeparatorLines = false;
            xAxis.UnitWidth = 0.5;
            xAxis.IsInverted = true;
            (xAxis as IAxis<SkiaSharpDrawingContext>).LabelsPaint = null;
            (yAxis as IAxis<SkiaSharpDrawingContext>).LabelsPaint = null;


            UpdateAudioLevelChartTimer = new Timer(UpdateAudioLevelChart, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }


        float ChartCurrentMaxAudioLevel = 0;
        public void NewAudioLevelReceived(object sender, float e)
        {
            ChartCurrentMaxAudioLevel = Math.Max(ChartCurrentMaxAudioLevel, e);
        }

        private void UpdateAudioLevelChart(object state)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var newMaxAudioLevel = ChartCurrentMaxAudioLevel;
                ChartCurrentMaxAudioLevel = 0;
                AudioLevelsHistory.Add(new ObservablePoint(AudioLevelHistoryIndex, newMaxAudioLevel + 0.01));
                SilenceThresholdHistory.Add(new ObservablePoint(AudioLevelHistoryIndex - 0.5, SilenceThreshold));
                AudioLevelHistoryIndex++;
                if (AudioLevelsHistory.Count > MaxAudioLevelHistoryLength)
                    AudioLevelsHistory.RemoveAt(0);
                if (SilenceThresholdHistory.Count > MaxAudioLevelHistoryLength + 1)
                    SilenceThresholdHistory.RemoveAt(0);
                if (IsUpdating)
                    UpdateAudioLevelChartTimer.Change(UpdateAudioChartTime, Timeout.InfiniteTimeSpan);
            });
        }
    }
}