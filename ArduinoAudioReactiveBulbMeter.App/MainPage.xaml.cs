using System.IO.Ports;

namespace ArduinoAudioReactiveBulbMeter.App
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void RefreshPorts_Clicked(object sender, EventArgs e)
        {
            InitializeSerialPorts();
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
