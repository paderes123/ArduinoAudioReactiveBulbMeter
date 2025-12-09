using System.IO.Ports;

namespace ArduinoAudioReactiveBulbMeter.App
{
    public partial class MainPage : ContentPage
    {
        private SerialPort? _serialPort;
        private bool isConnectedToArduino = false;

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
            AudioVisualizerSwitch.IsToggled = false;
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
