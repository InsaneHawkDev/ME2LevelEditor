using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ME2LevelEditor.Controls
{
    public class WaveformRenderer : FrameworkElement
    {
        public static readonly DependencyProperty WaveformSamplesProperty =
            DependencyProperty.Register(nameof(WaveformSamples), typeof(float[]), typeof(WaveformRenderer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ZoomLevelProperty =
            DependencyProperty.Register(nameof(ZoomLevel), typeof(double), typeof(WaveformRenderer),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ScrollOffsetSamplesProperty =
            DependencyProperty.Register(nameof(ScrollOffsetSamples), typeof(int), typeof(WaveformRenderer),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TotalSamplesProperty =
            DependencyProperty.Register(nameof(TotalSamples), typeof(int), typeof(WaveformRenderer),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AudioToAnalysisRatioProperty =
            DependencyProperty.Register(nameof(AudioToAnalysisRatio), typeof(double), typeof(WaveformRenderer),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public float[] WaveformSamples
        {
            get => (float[])GetValue(WaveformSamplesProperty);
            set => SetValue(WaveformSamplesProperty, value);
        }

        public double ZoomLevel
        {
            get => (double)GetValue(ZoomLevelProperty);
            set => SetValue(ZoomLevelProperty, value);
        }

        public int ScrollOffsetSamples
        {
            get => (int)GetValue(ScrollOffsetSamplesProperty);
            set => SetValue(ScrollOffsetSamplesProperty, value);
        }

        public int TotalSamples
        {
            get => (int)GetValue(TotalSamplesProperty);
            set => SetValue(TotalSamplesProperty, value);
        }

        public double AudioToAnalysisRatio
        {
            get => (double)GetValue(AudioToAnalysisRatioProperty);
            set => SetValue(AudioToAnalysisRatioProperty, value);
        }

        private WriteableBitmap _cachedBitmap;
        private byte[] _cachedPixels;
        private int _cachedWidth;
        private int _cachedHeight;

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            float[] samples = WaveformSamples;
            if (samples == null || samples.Length == 0 || ActualWidth <= 0 || ActualHeight <= 0)
                return;

            int width = (int)ActualWidth;
            int height = (int)ActualHeight;
            int stride = width * 4;

            if (_cachedBitmap == null || _cachedWidth != width || _cachedHeight != height)
            {
                _cachedBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                _cachedPixels = new byte[stride * height];
                _cachedWidth = width;
                _cachedHeight = height;
            }

            var bitmap = _cachedBitmap;
            byte[] pixels = _cachedPixels;
            Array.Clear(pixels, 0, pixels.Length);

            double zoom = ZoomLevel;
            int scrollOffset = ScrollOffsetSamples;
            double ratio = AudioToAnalysisRatio;
            int sampleCount = samples.Length;
            double halfHeight = height / 2.0;

            for (int x = 0; x < width; x++)
            {
                int analysisSample = (int)(x / zoom) + scrollOffset;
                int audioStart = (int)(analysisSample * ratio);
                int audioEnd = (int)((analysisSample + 1.0 / zoom) * ratio);

                audioStart = Math.Max(0, Math.Min(audioStart, sampleCount - 1));
                audioEnd = Math.Max(audioStart, Math.Min(audioEnd, sampleCount - 1));

                float min = 0f;
                float max = 0f;
                for (int i = audioStart; i <= audioEnd; i++)
                {
                    float s = samples[i];
                    if (s < min) min = s;
                    if (s > max) max = s;
                }

                int minY = (int)(halfHeight - max * halfHeight);
                int maxY = (int)(halfHeight - min * halfHeight);

                minY = Math.Max(0, Math.Min(height - 1, minY));
                maxY = Math.Max(0, Math.Min(height - 1, maxY));

                if (minY > maxY)
                {
                    int tmp = minY;
                    minY = maxY;
                    maxY = tmp;
                }

                for (int y = minY; y <= maxY; y++)
                {
                    double distFromCenter = Math.Abs(y - halfHeight) / halfHeight;
                    byte alpha = (byte)(40 + (int)(25 * distFromCenter));
                    int offset = y * stride + x * 4;
                    pixels[offset] = 255;
                    pixels[offset + 1] = 200;
                    pixels[offset + 2] = 0;
                    pixels[offset + 3] = alpha;
                }
            }

            bitmap.Lock();
            Marshal.Copy(pixels, 0, bitmap.BackBuffer, pixels.Length);
            bitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            bitmap.Unlock();

            dc.DrawImage(bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        }
    }
}
