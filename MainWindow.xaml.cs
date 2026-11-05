using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using NAudio.Wave;
using GuitarTuner.Logic;

namespace GuitarTuner
{
    public partial class MainWindow : Window
    {
        private WaveInEvent? _waveIn;
        private bool _isListening = false;

        private readonly List<float[]> _buffers = new();
        private const int SampleRate = 44100;
        private int _a4Frequency = 440;

        private double _currentVisualPos = 0;
        private double _targetVisualPos = 0;

        private double _smoothedFreq = 0;

        private string _lastNote = "";

        public MainWindow()
        {
            InitializeComponent();

            LoadMicrophones();

            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_isListening) return;

            double smoothingFactor = 0.15;

            _currentVisualPos += (_targetVisualPos - _currentVisualPos) * smoothingFactor;

            if (Math.Abs(_targetVisualPos - _currentVisualPos) < 0.001)
                _currentVisualPos = _targetVisualPos;

            double maxShift = 350.0;
            ball.RenderTransform = new TranslateTransform(_currentVisualPos * maxShift, 0);
        }

        private void StartStopClick(object sender, RoutedEventArgs e)
        {
            if (_isListening) StopListening();
            else StartListening();
        }

        private void StartListening()
        {
            _isListening = true;
            start_stop_button.Content = "Остановить";
            start_stop_button.Background = new SolidColorBrush(Color.FromRgb(192, 57, 43));

            if (mic_combo_box.SelectedIndex < 0)
            {
                MessageBox.Show("Микрофон не выбран. Выберите устройство в настройках");
                StopListening();
                return;
            }

            _waveIn = new WaveInEvent()
            {
                DeviceNumber = mic_combo_box.SelectedIndex,
                WaveFormat = new WaveFormat(SampleRate, 16, 1),
                BufferMilliseconds = 30
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
        }

        private void StopListening()
        {
            _isListening = false;
            start_stop_button.Content = "Начать";
            start_stop_button.Background = new SolidColorBrush(Color.FromRgb(50, 205, 50));

            if (_waveIn != null)
            {
                _waveIn.StopRecording();
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.Dispose();
                _waveIn = null;
            }

            note_text.Text = "—";
            note_hz_text.Text = "(—)";
            current_hz_text.Text = "Текущая частота: —";
            deviation_text.Text = "Отклонение: —";
            _currentVisualPos = 0;
            _targetVisualPos = 0;
            ball.RenderTransform = new TranslateTransform(0, 0);
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            int sampleCount = e.BytesRecorded / 2;
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                short sample = BitConverter.ToInt16(e.Buffer, i * 2);
                samples[i] = sample / 32768f;
            }

            lock (_buffers)
            {
                _buffers.Add(samples);

                if (_buffers.Count >= 4)
                {
                    int totalLength = _buffers.Sum(b => b.Length);
                    float[] combined = new float[totalLength];
                    int offset = 0;
                    foreach (var b in _buffers)
                    {
                        b.CopyTo(combined, offset);
                        offset += b.Length;
                    }
                    _buffers.Clear();

                    Dispatcher.Invoke(() => ProcessAudio(combined));
                }
            }
        }

        private void ProcessAudio(float[] buffer)
        {
            double rms = Math.Sqrt(buffer.Average(x => x * x));
            if (rms < 0.003) return;

            double freq = PitchDetector.DetectPitch(buffer, SampleRate);
            if (freq < 60 || freq > 1300) return;

            var (newNote, _) = NoteAnalyzer.Analyze(freq, _a4Frequency);

            if (_lastNote != newNote)
            {
                _smoothedFreq = freq;
                _lastNote = newNote;
            }
            else
            {
                if (_smoothedFreq > 0 && Math.Abs(freq - _smoothedFreq) / _smoothedFreq > 0.15) return;

                if (_smoothedFreq == 0) _smoothedFreq = freq;
                else _smoothedFreq += (freq - _smoothedFreq) * 0.25;
            }

            var (note, targetFreq) = NoteAnalyzer.Analyze(_smoothedFreq, _a4Frequency);
            note_text.Text = note;
            note_hz_text.Text = $"({targetFreq:F1} Гц)";
            current_hz_text.Text = $"Текущая частота: {_smoothedFreq:F1} Гц";

            double semitonesDiff = 12 * Math.Log2(_smoothedFreq / targetFreq);
            double cents = semitonesDiff * 100;
            double displayCents = Math.Clamp(cents, -50, 50);
            _targetVisualPos = displayCents / 50.0;

            if (Math.Abs(displayCents) < 1) deviation_text.Text = "Отклонение: 0 центов";
            else deviation_text.Text = $"Отклонение: {displayCents:+0;-0;0} центов";
        }

        private void LoadMicrophones()
        {
            mic_combo_box.Items.Clear();

            for (int i = 0; i < WaveIn.DeviceCount; i++)
            {
                var caps = WaveIn.GetCapabilities(i);
                mic_combo_box.Items.Add(caps.ProductName);
            }

            if (mic_combo_box.Items.Count > 0) mic_combo_box.SelectedIndex = 0;
        }

        private void PlusA4Click(object sender, RoutedEventArgs e)
        {
            if (_a4Frequency < 460) _a4Frequency++;
            A4_hz_text.Text = _a4Frequency.ToString();
        }

        private void MinusA4Click(object sender, RoutedEventArgs e)
        {
            if (_a4Frequency > 420) _a4Frequency--;
            A4_hz_text.Text = _a4Frequency.ToString();
        }

        private void ResetSettingsClick(object sender, RoutedEventArgs e)
        {
            mic_combo_box.SelectedIndex = 0;
            _a4Frequency = 440;
            A4_hz_text.Text = "440";
        }
    }
}