using BulkPhotoProcessing.Helpers;
using DirectShowLib;
using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
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
        private ObservableCollection<string> _cameras = new ObservableCollection<string>();
        private Dispatcher dispatcher;
        private bool isCapturing = false, isFaceCaptured;
        private VideoCapture cap;
        private int _selectedIndex = 0, _picturesTaken = 0;
        private Image<Bgr, byte> _faceImage;

        public bool IsCapturing { get { return isCapturing; } set { isCapturing = value; TakePicture.RaiseCanExecuteChanged(); StartCapture.RaiseCanExecuteChanged(); } }
        public bool IsFaceCaptured { get { return isFaceCaptured; } set { isFaceCaptured = value; TakePicture.RaiseCanExecuteChanged(); StartCapture.RaiseCanExecuteChanged(); } }
        

        public ObservableCollection<string> Cameras { get { return _cameras; } set { _cameras = value; NotifyPropertyChanged(); } }
        public int SelectedIndex { get { return _selectedIndex; } set { _selectedIndex = value; NotifyPropertyChanged(); } }
        public RelayCommand StartCapture { get; set; }
        public RelayCommand TakePicture { get; set; }

        public RealTimeProcessingViewmodel()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            foreach (var device in DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice))
            {
                Cameras.Add(device.Name);
            }
            if (Cameras.Count == 0) MessageBox.Show("K počítači nejsou připojeny žádné kamery!");
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
                    IsCapturing = true;
                },
                () => { return !IsCapturing; });
            TakePicture = new RelayCommand(
                () =>
                {
                    string path = Path.Combine(Environment.CurrentDirectory, "TakenPictures");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    CvInvoke.Resize(_faceImage, _faceImage, new System.Drawing.Size(200, 300), interpolation: Emgu.CV.CvEnum.Inter.Cubic);
                    _faceImage.Save(Path.Combine(path, "Picture " + _picturesTaken + ".jpg"));
                    _picturesTaken++;
                },
                () => { return IsCapturing; });
        }

        private void FrameCaptured(object sender, EventArgs e)
        {
            Mat m = new Mat();
            cap.Retrieve(m);
            Image<Bgr, byte> img = m.ToImage<Bgr, byte>();


            //Convert Bgr image to Gray image
            Mat gray = new Mat();
            CvInvoke.CvtColor(img, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);

            //Detecting faces on the image
            Rectangle[] faces = Helper.cascadeClassifier.DetectMultiScale(gray, 1.2, 7, new System.Drawing.Size(30, 30));
            if (faces.Length > 0)
            {
                _faceImage = img.CopyBlank();
                img.CopyTo(_faceImage);
                foreach (var face in faces)
                {
                    //Setting the reign of interest around the face, so we can take picture of it
                    _faceImage.ROI = new Rectangle(face.X - 25, face.Y - 75, face.Width + 50, face.Height + 150);
                    CvInvoke.Rectangle(img, new Rectangle(face.X - 25, face.Y - 75, face.Width + 50, face.Height + 150), new MCvScalar(0, 0, 255), 2);
                }
            }
            CvInvoke.Imshow("Video", img);
            if (CvInvoke.WaitKey(1) == 'q')
            {
                cap.Stop();
                cap.Dispose();
                dispatcher.Invoke(() => IsCapturing = false);
                CvInvoke.DestroyWindow("Video");
            }
        }


    }

}
