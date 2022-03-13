using BulkPhotoProcessing.Helpers;
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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UWPBindingCollection.ViewModels;

namespace BulkPhotoProcessing.Viewmodels
{
    public class PhotoProcessingViewmodel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private Dispatcher dispatcher;

        private ObservableCollection<string> _names = new ObservableCollection<string>();
        private ObservableCollection<Image> _images = new ObservableCollection<Image>();
        private List<string> _files = new List<string>();
        private bool namesChosen = false, picturesChosen = false;
        private string _errorPictures = "";

        public ObservableCollection<string> Names { get { return _names; } set { _names = value; NotifyPropertyChanged(); } }
        public ObservableCollection<Image> Images { get { return _images; } set { _images = value; NotifyPropertyChanged(); } }

        public RelayCommand AddPhotos { get; set; }
        public RelayCommand AddNamesList { get; set; }
        public RelayCommand ProcessPhotos { get; set; }
        public PhotoProcessingViewmodel()
        {
            dispatcher = Dispatcher.CurrentDispatcher;
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
                        foreach (var path in dialog.FileNames)
                        {
                            //Load selected image
                            Image<Bgr, byte> colored = new Image<Bgr, byte>(path);

                            //Recognize face on the image so we can cut it around later
                            Mat gray = new Mat();
                            CvInvoke.CvtColor(colored, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                            System.Drawing.Rectangle[] faces = Helper.cascadeClassifier.DetectMultiScale(gray, 1.3, 7, new System.Drawing.Size(30, 30));
                            if (faces.Length == 1)
                            {
                                Image image = new Image();

                                //Convert EmguCV format to BitmapSource format, so the wpf controls can display it
                                image.Source = Helper.ToBitmapSource(colored);

                                //Add image to the list of images
                                Dispatcher.CurrentDispatcher.Invoke(() => { Images.Add(image); });
                                _files.Add(path);

                                picturesChosen = true;
                            }
                            else
                            {
                                _errorPictures += $"na {path.Split("\\").Last()} nebyl detekován jeden obličej ! {Environment.NewLine}";
                            }
                        }

                        if (_errorPictures != "")
                        {
                            MessageBox.Show(_errorPictures);
                        }
                        ProcessPhotos.RaiseCanExecuteChanged();
                    };
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
                        //Sort the names by alphabet
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
                        MessageBox.Show($"Počet fotek a jmen nesedí.\r\nZkontrolujte svoje seznamy a zkuste to znovu. \r\npočet fotek: {_images.Count} \r\npočet jmen: {_names.Count}");
                        return;
                    }
                    for (int i = 0; i < _files.Count; i++)
                    {
                        try
                        {
                            //check if output folder exists if not then create it
                            string path = Path.Combine(Environment.CurrentDirectory, "Output");
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            string file = Path.Combine(path, Names[i] + "." + _files[i].Split('.').Last());
                            Image<Bgr, byte> image = new Image<Bgr, byte>(_files[i]);
                            CvInvoke.Resize(image, image, new System.Drawing.Size(200, 300), interpolation: Emgu.CV.CvEnum.Inter.Cubic);
                            image.Save(file);

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
    }
}
