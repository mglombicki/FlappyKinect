using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KinectSensor sensor;
        private BodyFrameReader bodyFrameReader;
        private ColorFrameReader colorFrameReader;
        private double birdHeight;
        private double prevRightHandHeight;
        private double prevLeftHandHeight;
        private double pipeX;
        private double pipeY;

        public MainWindow()
        {
            // initialize the components (controls) of the window
            InitializeComponent();

            birdHeight = this.Height / 2;
            prevRightHandHeight = 0;
            prevLeftHandHeight = 0;
            pipeX = -100;
            pipeY = 0;

            // Get the sensor
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            // Setup readers for each source of data we want to use
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
            colorFrameReader = sensor.ColorFrameSource.OpenReader();

            // Setup event handlers that use what we get from the readers
            bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;
            colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
        }

        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // Just for debugging
            TimeLabel.Content = DateTime.Now.Second + "." + DateTime.Now.Millisecond;

            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    var colorFrameDescription = colorFrame.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
                    var frameSize = colorFrameDescription.Width * colorFrameDescription.Height * colorFrameDescription.BytesPerPixel;
                    var colorData = new byte[frameSize];
                    if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                    {
                        colorFrame.CopyRawFrameDataToArray(colorData);
                    }
                    else
                    {
                        colorFrame.CopyConvertedFrameDataToArray(colorData, ColorImageFormat.Bgra);
                    }

                    ColorImage.Source = BitmapSource.Create(
                        colorFrame.ColorFrameSource.FrameDescription.Width,
                        colorFrame.ColorFrameSource.FrameDescription.Height,
                        96, 96, PixelFormats.Bgr32, null, colorData, colorFrameDescription.Width * 4);
                }
            }
        }

        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }

                Body[] bodies = new Body[bodyFrame.BodyCount];
                bodyFrame.GetAndRefreshBodyData(bodies);
                foreach (Body body in bodies)
                {
                    if (body.IsTracked)
                    {
                        var joints = body.Joints;
                        if (joints[JointType.HandRight].TrackingState == TrackingState.Tracked
                            && joints[JointType.HandLeft].TrackingState == TrackingState.Tracked)
                        {
                            var rightHandFlap = Math.Max(0, prevRightHandHeight - joints[JointType.HandRight].Position.Y);
                            var leftHandFlap = Math.Max(0, prevLeftHandHeight - joints[JointType.HandLeft].Position.Y);
                            birdHeight -= 100*(rightHandFlap + leftHandFlap);
                            FlapLabel.Content = (int)(100 * (rightHandFlap + leftHandFlap)); // For debugging

                            prevRightHandHeight = joints[JointType.HandRight].Position.Y;
                            prevLeftHandHeight = joints[JointType.HandLeft].Position.Y;
                        }
                        else
                        {
                            FlapLabel.Content = "Not tracked";
                        }
                    }
                }
            }

            // Move the bird
            birdHeight += 4; // Gravity
            birdHeight = Math.Max(0, Math.Min(birdHeight, this.Height - 60));
            BirdShape.Margin = new Thickness(0, birdHeight, 0, 0);

            // Move the pipe
            pipeX -= 4;
            if (pipeX < -100)
            {
                pipeX = this.Width;
                // Flip the pipe position
                if (pipeY == 0)
                {
                    pipeY = -360;
                }
                else
                {
                    pipeY = 0;
                }
            }
            PipeShape.Margin = new Thickness(pipeX, pipeY, 0, 0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Close the sensor when we close the window (and the application)
            sensor.Close();
        }
    }
}