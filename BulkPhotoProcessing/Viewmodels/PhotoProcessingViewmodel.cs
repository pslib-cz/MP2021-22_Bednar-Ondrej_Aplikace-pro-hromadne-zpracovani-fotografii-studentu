using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using UWPBindingCollection.ViewModels;

namespace BulkPhotoProcessing.Viewmodels
{
    internal class PhotoProcessingViewmodel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        static readonly CascadeClassifier cascadeClassifier = new CascadeClassifier("haarcascade_frontalface_alt.xml");
        private ObservableCollection<string> _names = new ObservableCollection<string>();
        private ObservableCollection<Image> _images = new ObservableCollection<Image>();
        private List<string> _files = new List<string>();
        private bool namesChosen = false, picturesChosen = false;

        public ObservableCollection<string> Names { get { return _names; } set { _names = value; NotifyPropertyChanged(); } }
        public ObservableCollection<Image> Images { get { return _images; } set { _images = value; NotifyPropertyChanged(); } }

        public RelayCommand AddPhotos { get; set; }
        public RelayCommand AddNamesList { get; set; }
        public RelayCommand ProcessPhotos { get; set; }
        public PhotoProcessingViewmodel()
        {
            AddPhotos = new RelayCommand(
                () =>
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Multiselect = true;
                    dialog.DefaultExt = ".png";
                    dialog.Filter = "Image Files (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";

                    if (dialog.ShowDialog() == true)
                    {
                        Images.Clear();
                        _files.Clear();
                        foreach (string path in dialog.FileNames) 
                        {
                            Image<Bgr, byte> colored = new Image<Bgr, byte>(path);
                            Mat gray = new Mat();
                            CvInvoke.CvtColor(colored, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                            System.Drawing.Rectangle[] faces = cascadeClassifier.DetectMultiScale(gray, 1.2, 7, new System.Drawing.Size(30, 30));
                            if (faces.Length > 0)
                            {
                                Image image = new Image();
                                image.Width = 200;
                                image.Height = 100;

                                image.Source = ToBitmapSource(colored);
                                Images.Add(image);
                                _files.Add(path);

                                picturesChosen = true;
                            }
                        }
                        ProcessPhotos.RaiseCanExecuteChanged();
                    }
                },
                () => true);
            AddNamesList = new RelayCommand(
                () =>
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Multiselect = false;
                    dialog.DefaultExt = ".csv";
                    dialog.Filter = "Coma separated values (.csv)|*.csv*";

                    if (dialog.ShowDialog() == true)
                    {
                        Names.Clear();
                        string[] n = File.ReadAllText(dialog.FileName).Split(',');
                        Array.Sort(n, StringComparer.InvariantCulture);

                        foreach (var name in n)
                        {
                            if (name == "\r\n") continue;
                            Names.Add(name.Split("\r\n")[1]);
                        }

                        namesChosen = true;
                        ProcessPhotos.RaiseCanExecuteChanged();

                    }
                },
                () => true);
            ProcessPhotos = new RelayCommand(
                async () =>
                {
                    if (Images.Count != Names.Count)
                    {
                        MessageBox.Show("Počet fotek a jmen nesedí.\r\nZkontrolujte svoje seznamy a zkuste to znovu");
                        return;
                    }
                    for (int i = 0; i < _files.Count; i++)
                    {
                        try
                        {
                            string path = Path.Combine(Environment.CurrentDirectory, "Output");
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            string file = Path.Combine(path, Names[i] + "." + _files[i].Split('.').Last());
                            using (var fs = new FileStream(file, FileMode.Create))
                            {
                                await File.Open(_files[i], FileMode.Open, FileAccess.Read).CopyToAsync(fs);
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                },
                () =>
                {
                    if (namesChosen && picturesChosen) return true;
                    return false;
                });

        }
        //Konvertor převzaný z https://gist.github.com/eklimcz-zz/4125797
        [DllImport("gdi32")]
        private static extern int DeleteObject(IntPtr o);
        public static BitmapSource ToBitmapSource(Emgu.CV.Image<Bgr, byte> image)
        {
            using (System.Drawing.Bitmap source = image.ToBitmap())
            {
                IntPtr ptr = source.GetHbitmap(); //obtain the Hbitmap

                BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                    ptr,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
                

                DeleteObject(ptr); //release the HBitmap
                return bs;
            }
        }
    }
}
