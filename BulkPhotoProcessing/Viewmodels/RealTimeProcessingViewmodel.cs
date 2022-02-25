using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using UWPBindingCollection.ViewModels;

namespace BulkPhotoProcessing.Viewmodels
{
    internal class RealTimeProcessingViewmodel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private BitmapImage _capturedFrame;
        private ObservableCollection<string> _cameras = new ObservableCollection<string>();
        private bool IsCapturing = false;
        private VideoCapture cap;
        private int _selectedIndex = 0;

        public ObservableCollection<string> Cameras { get { return _cameras; } set { _cameras = value;NotifyPropertyChanged(); } }
        public BitmapImage CapturedFrame { get { return _capturedFrame; } set { _capturedFrame = value; NotifyPropertyChanged(); } }
        public int SelectedIndex { get { return _selectedIndex; } set { _selectedIndex = value; NotifyPropertyChanged();} }
        public RelayCommand StartCapture { get; set; }


        public RealTimeProcessingViewmodel()
        {
            try
            {
            foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
            {
                Cameras.Add(device.Name);
            }
            }
            catch (Exception)
            {
                MessageBox.Show("K počítači nejsou připojeny žádné kamery!");
            }
            StartCapture = new RelayCommand(
                () =>
                {
                    cap = new VideoCapture(SelectedIndex);
                    if (!cap.IsOpened)
                    {
                        return;
                    }
                    cap.ImageGrabbed += FrameCaptured;
                    cap.Start();
                },
                () => { return !IsCapturing; });
        }

        private void FrameCaptured(object sender, EventArgs e)
        {
            Mat m = new Mat();
            cap.Retrieve(m);
            Image<Bgr, byte> img = m.ToImage<Bgr, byte>();


            //Convert Bgr image to Gray image
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            //Enhancing the image to get better result
            CvInvoke.EqualizeHist(gray, gray);

            Rectangle[] faces = cascadeClassifier.DetectMultiScale(gray, 1.2, 7, new System.Drawing.Size(30, 30));
            if (faces.Length > 0)
            {
                foreach (var face in faces)
                {
                    CvInvoke.Rectangle(img, new Rectangle(face.X - 25, face.Y - 70, 250, 350), new MCvScalar(0, 0, 255), 2);
                }
            }
            CvInvoke.Imshow("Frame", img);
            if (CvInvoke.WaitKey(1) == 'q')
            {
                cap.Stop();
                cap.Dispose();
                CvInvoke.DestroyWindow("Frame");
            }
        }
    }

}
