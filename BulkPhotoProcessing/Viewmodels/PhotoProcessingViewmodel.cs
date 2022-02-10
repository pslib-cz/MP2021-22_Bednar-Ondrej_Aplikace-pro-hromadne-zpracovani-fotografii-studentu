using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
                            Image image = new Image();
                            image.Width = 200;
                            image.Height = 100;

                            BitmapImage img = new BitmapImage();
                            img.BeginInit();
                            img.CacheOption = BitmapCacheOption.OnLoad;
                            img.UriSource = new Uri(path);
                            img.EndInit();

                            image.Source = img;
                            Images.Add(image);
                            _files.Add(path);
                        }
                        picturesChosen = true;
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
                            if (!Directory.Exists(Path.Combine(Environment.CurrentDirectory, "Output")))
                            {
                                Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, "Output"));
                            }
                            string path = Path.Combine(Environment.CurrentDirectory, "Output", Names[i] + "." + _files[i].Split('.').Last());
                            using (var fs = new FileStream(path, FileMode.Create))
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
    }
}
