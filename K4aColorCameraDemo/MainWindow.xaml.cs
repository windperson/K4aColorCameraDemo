using K4AdotNet.Sensor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace K4aColorCameraDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device;

        private DeviceConfiguration deviceConfig;
        private ImageVisualizer colorImageVisualizer;
        private CancellationTokenSource cameraIsRecordingCancellationTokenSource;

        public MainWindow()
        {
            InitializeComponent();
        }

        private DeviceConfiguration CreateCameraConfig()
        {
            DeviceConfiguration config = DeviceConfiguration.DisableAll;
            config.CameraFps = FrameRate.Thirty;
            config.ColorFormat = ImageFormat.ColorBgra32;
            config.ColorResolution = ColorResolution.R2160p;
            config.DepthMode = DepthMode.NarrowViewUnbinned;
            config.SynchronizedImagesOnly = true;
            return config;
        }

        private async void StartCamera(CancellationToken cancelToken)
        {
            try
            {
                device = Device.Open();
                device.StartCameras(deviceConfig);
                txtResult.Dispatcher.Invoke(() =>
                {
                    txtResult.Text = "camera started";
                });
                while (!cancelToken.IsCancellationRequested)
                {
                    using Capture capture = await Task.Run(() => { return device.GetCapture(); }, cancelToken);
                    _resultTxt.Dispatcher.Invoke(() =>
                    {
                        _resultTxt.Text = $"capture data: {capture.DepthImage.DeviceTimestamp}";
                    });

                    if (capture.ColorImage != null && colorImageVisualizer.Update(capture.ColorImage))
                    {
                        displayImg.Dispatcher.Invoke(() =>
                        {
                            displayImg.Source = colorImageVisualizer.ImageSource;
                        }, System.Windows.Threading.DispatcherPriority.Render, cancelToken);
                    }
                    //magic delay to make preview smooth.
                    Task.Delay(10, cancelToken).Wait();
                }
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex.InnerException is TaskCanceledException)
            {
                //do nothing
            }
            catch(Exception ex)
            {
                txtResult.Dispatcher.Invoke(() =>
                {
                    txtResult.Text = $"Failed to start camera, ex={ex}";
                });
                return;
            }
        }

        private void StopCamera()
        {
            cameraIsRecordingCancellationTokenSource.Cancel();
            device.Dispose();
            device = null;
        }

        private void ShowInfo()
        {
            Device device = Device.Open();
            txtResult.Text = $"1st Device s/n: {device.SerialNumber}";
            device.Dispose();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowInfo();
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (device != null)
            {
                device.Dispose();
            }
        }

        private void Btn_start_Click(object sender, RoutedEventArgs e)
        {
            btn_start.IsEnabled = false;
            btn_stop.IsEnabled = true;
            deviceConfig = CreateCameraConfig();
            colorImageVisualizer = ImageVisualizer.CreateForColorBgra(System.Windows.Threading.Dispatcher.CurrentDispatcher, deviceConfig.ColorResolution);
            cameraIsRecordingCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => StartCamera(cameraIsRecordingCancellationTokenSource.Token));
        }

        private void Btn_stop_Click(object sender, RoutedEventArgs e)
        {
            StopCamera();
            btn_start.IsEnabled = true;
            btn_stop.IsEnabled = false;
        }
    }
}
