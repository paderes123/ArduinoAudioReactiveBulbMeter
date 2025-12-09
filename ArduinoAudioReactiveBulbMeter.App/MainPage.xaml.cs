using NAudio.CoreAudioApi;
using System.IO.Ports;

namespace ArduinoAudioReactiveBulbMeter.App
{
    public partial class MainPage : ContentPage
    {
        private SerialPort? _serialPort;
        private bool isConnectedToArduino = false;
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly Dictionary<string, MMDevice> audioDevices = new();
        private Task? _audioVisualizerTask;

        public MainPage()
        {
            InitializeComponent();
        }

        private void RefreshPorts_Clicked(object sender, EventArgs e)
        {
            InitializeSerialPorts();
        }

        private async Task ConnectToArduino()
        {
            try
            {
                string? selectedSerialPort = SerialPortPicker.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedSerialPort))
                {
                    await DisplayAlertAsync("No Port Selected", "Please select a serial port!", "Ok");
                    return;
                }

                _serialPort = new SerialPort(selectedSerialPort, 9600);
                _serialPort.Open();
                ConnectButton.Text = "Disconnect";
                LightControlPanel.IsEnabled = true;
                isConnectedToArduino = true;
            }
            catch (UnauthorizedAccessException)
            {
                await DisplayAlertAsync("Busy", "The selected serial port is busy!", "Ok");
            }
            catch (NullReferenceException)
            {
                await DisplayAlertAsync("Empty Serial Port", "There is no serial port!", "Ok");
            }
            catch (Exception e)
            {
                await DisplayAlertAsync("Error", e.ToString(), "Ok");
            }
        }

        private MMDevice GetSelectedAudioDevice(string selectedAudioDeviceName, Dictionary<string, MMDevice> audioDevices)
        {
            if (audioDevices.TryGetValue(selectedAudioDeviceName, out var selectedDevice))
                return selectedDevice;

            throw new InvalidOperationException("Selected device not found.");
        }

        internal async Task PerformAudioVisualizerAsync(int numOfLeds, CancellationToken cancellationToken)
        {
            if (_serialPort == null || !_serialPort.IsOpen) return;

            numOfLeds += 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (!this.IsVisible || AudioDevicePicker == null || audioDevices == null)
                    return;

                MMDevice? selectedAudioDevice = null;
                try
                {
                    selectedAudioDevice = GetSelectedAudioDevice(AudioDevicePicker.SelectedItem?.ToString() ?? "", audioDevices);
                }
                catch
                {
                    await Task.Delay(50);
                    continue;
                }

                if (selectedAudioDevice == null)
                {
                    await Task.Delay(50);
                    continue;
                }

                float volumeLevel = selectedAudioDevice.AudioMeterInformation.MasterPeakValue;
                int intensityLevel = (int)(volumeLevel * numOfLeds);
                intensityLevel = Math.Max(0, Math.Min(numOfLeds - 1, intensityLevel));

                string intensityString = intensityLevel.ToString();

                try
                {
                    await Task.Run(() =>
                    {
                        if (_serialPort != null && _serialPort.IsOpen)
                            _serialPort.WriteLine(intensityString);
                    });
                }
                catch { /* ignore if app is closing */ }

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    UpdateLedIndicators(intensityLevel, numOfLeds);
                });

                await Task.Delay(5);
            }
        }

        private void UpdateLedIndicators(int intensityLevel, int numOfLeds)
        {
            for (int i = 1; i <= numOfLeds; i++)
            {
                if (i <= intensityLevel)
                {
                    if (i <= 3)
                        SetLedColor(i, Colors.Green);
                    else if (i <= 6)
                        SetLedColor(i, Colors.Blue);
                    else
                        SetLedColor(i, Colors.Red);
                }
                else
                {
                    SetLedColor(i, Colors.Transparent);
                }
            }
        }

        private async Task DisconnectFromArduino()
        {
            ConnectButton.Text = "Connect";
            LightControlPanel.IsEnabled = false;
            isConnectedToArduino = false;

            if (_serialPort != null && _serialPort.IsOpen)
                _serialPort.Close();

            await StopAudioVisualizerAsync();
            ClearLightIndicators();
            AudioVisualizerSwitch.IsToggled = false;
        }

        private async Task StopAudioVisualizerAsync()
        {
            if (_audioVisualizerTask != null && !_audioVisualizerTask.IsCompleted)
            {
                _cancellationTokenSource?.Cancel();
                try
                {
                    await _audioVisualizerTask;
                }
                catch (OperationCanceledException) { }
            }
        }

        private void SetLedColor(int ledNumber, Color color)
        {
            switch (ledNumber)
            {
                case 1: Light1.BackgroundColor = color; break;
                case 2: Light2.BackgroundColor = color; break;
                case 3: Light3.BackgroundColor = color; break;
                case 4: Light4.BackgroundColor = color; break;
                case 5: Light5.BackgroundColor = color; break;
                case 6: Light6.BackgroundColor = color; break;
                case 7: Light7.BackgroundColor = color; break;
                case 8: Light8.BackgroundColor = color; break;
            }
        }

        private void ClearLightIndicators()
        {
            for (int i = 1; i <= 8; i++)
            {
                SetLedColor(i, Colors.Transparent);
            }
        }

        private MMDeviceCollection GetAvailableSoundOutput()
        {
            var enumerator = new MMDeviceEnumerator();
            return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }

        private async void DisplayAvailableSoundOutput()
        {
            AudioDevicePicker.Items.Clear();
            audioDevices.Clear();

            var devices = GetAvailableSoundOutput();

            if (devices.Count > 0)
            {
                foreach (var device in devices)
                {
                    try
                    {
                        string name = device.FriendlyName;
                        AudioDevicePicker.Items.Add(name);
                        audioDevices[name] = device;
                    }
                    catch (System.Runtime.InteropServices.COMException ex)
                    {
                        await DisplayAlertAsync("Error", $"Skipping audio device due to COM error: {ex.Message}", "Ok");
                        continue;
                    }
                }

                if (AudioDevicePicker.Items.Count > 0)
                    AudioDevicePicker.SelectedIndex = 0;
                else
                    AudioDevicePicker.Title = "No Usable Devices";
            }
            else
            {
                AudioDevicePicker.Title = "No Output Devices Found";
            }
        }

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            if (!isConnectedToArduino)
                await ConnectToArduino();
            else
                await DisconnectFromArduino();
        }

        private void RefreshAudioOutputDevicesButton_Click(object sender, EventArgs e)
        {
            DisplayAvailableSoundOutput();
        }

        private void InitializeSerialPorts()
        {
            SerialPortPicker.Items.Clear();
            var ports = SerialPort.GetPortNames();

            if (ports.Length > 0)
            {
                foreach (var port in ports)
                {
                    SerialPortPicker.Items.Add(port);
                }

                SerialPortPicker.SelectedIndex = 0;
                SerialPortPicker.IsEnabled = true;
            }
            else
            {
                SerialPortPicker.Title = "No Ports Available";
                SerialPortPicker.IsEnabled = false;
            }
        }
    }
}
