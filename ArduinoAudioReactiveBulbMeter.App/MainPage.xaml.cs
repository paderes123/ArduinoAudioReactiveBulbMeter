using System.IO.Ports;

namespace ArduinoAudioReactiveBulbMeter.App
{
    public partial class MainPage : ContentPage
    {
        private SerialPort? _serialPort;
        private bool isConnectedToArduino = false;
        private CancellationTokenSource _cancellationTokenSource = new();
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

        private async void ConnectButton_Clicked(object sender, EventArgs e)
        {
            if (!isConnectedToArduino)
                await ConnectToArduino();
            else
                await DisconnectFromArduino();
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
