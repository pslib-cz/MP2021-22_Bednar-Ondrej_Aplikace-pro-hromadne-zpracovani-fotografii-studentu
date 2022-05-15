using BulkPhotoProcessing.Helpers;
using BulkPhotoProcessing.NewFolder;
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
using System.Windows.Media;
using System.Windows.Threading;
using UWPBindingCollection.ViewModels;

namespace BulkPhotoProcessing.Viewmodels
{
    public class PhotoProcessingViewmodel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<string> _names = new ObservableCollection<string>();
        private ObservableCollection<Image> _images = new ObservableCollection<Image>();
        private ObservableCollection<ImageNames> _imgNames = new ObservableCollection<ImageNames>();
        private List<string> _files = new List<string>();
        private bool namesChosen = false, picturesChosen = false, preProcessed = false;
        private string _errorPictures = "";
        private ImageNames _imgName;
        private string _name;
        private ImageSource _img;

        public ObservableCollection<string> Names { get { return _names; } set { _names = value; NotifyPropertyChanged(); } }
        public ObservableCollection<Image> Images { get { return _images; } set { _images = value; NotifyPropertyChanged(); } }
        public ObservableCollection<ImageNames> ImageNames { get { return _imgNames; } set { _imgNames = value; NotifyPropertyChanged();} }
        public ImageNames SelectedImg { get { return _imgName; } set { _imgName = value; NotifyPropertyChanged(); } } 
        public bool PreProcessed { get { return preProcessed; } set { preProcessed = value; ProcessPhotos.RaiseCanExecuteChanged(); } }
        public bool PicturesChosen { get { return picturesChosen; } set { picturesChosen = value; PreProcessPhotos.RaiseCanExecuteChanged(); } }
        public bool NamesChosen { get { return namesChosen; } set { namesChosen = value; PreProcessPhotos.RaiseCanExecuteChanged(); } }
        public string Name { get { return _name; } set { _name = value; NotifyPropertyChanged(); AddPerson.RaiseCanExecuteChanged(); } }
        public ImageSource Image { get { return _img; } set { _img = value; NotifyPropertyChanged(); AddPerson.RaiseCanExecuteChanged(); } }
        
        public RelayCommand AddPhotos { get; set; }
        public RelayCommand AddNamesList { get; set; }
        public RelayCommand PreProcessPhotos { get; set; }
        public RelayCommand ProcessPhotos { get; set; }
        public RelayCommand ChangePhoto { get; set; }
        public RelayCommand AddPerson { get; set; }
        public RelayCommand RemovePerson { get; set; }
        public RelayCommand EditPerson { get; set; }
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
                        _errorPictures = "";
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
                                Images.Add(image);
                                _files.Add(path);

                                PicturesChosen = true;
                            }
                            else
                            {
                                _errorPictures += $"Na {path.Split("\\").Last()} nebyl detekován jeden obličej ! {Environment.NewLine}";
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
                        n[0] = "\r\n" + n[0].Split("\r\n")[1];
                        Array.Sort(n, StringComparer.InvariantCulture);

                        foreach (var name in n)
                        {
                            if (name == "\r\n") continue;
                            Names.Add(name.Split("\r\n")[1]);
                        }

                        NamesChosen = true;
                        ProcessPhotos.RaiseCanExecuteChanged();

                    }
                },
                () => true);
            PreProcessPhotos = new RelayCommand(
                () =>
                {
                    ImageNames.Clear();
                    if (Images.Count != Names.Count)
                    {
                        MessageBox.Show($"Počet fotek a jmen nesedí.\r\nZkontrolujte svoje seznamy a zkuste to znovu. \r\npočet fotek: {_images.Count} \r\npočet jmen: {_names.Count}");
                        return;
                    }
                    for (int i = 0; i < _files.Count; i++)
                    {
                        ImageNames s = new ImageNames()
                        {
                            Name = _names[i],
                            Image = _images[i].Source
                        };
                        ImageNames.Add(s);
                        PreProcessed = true;
                    }
                },
                () =>
                {
                    if (NamesChosen && PicturesChosen) return true;
                    return false;
                });
            ProcessPhotos = new RelayCommand(
                () =>
                {
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
                            Mat gray = new Mat();

                            CvInvoke.CvtColor(image, gray, Emgu.CV.CvEnum.ColorConversion.Bgr2Gray);
                            System.Drawing.Rectangle[] faces = Helper.cascadeClassifier.DetectMultiScale(gray, 1.3, 7, new System.Drawing.Size(30, 30));
                            foreach (var face in faces)
                            {
                                image.ROI = new System.Drawing.Rectangle(face.X - 25, face.Y - 75, face.Width + 50, face.Height + 150);
                                CvInvoke.Resize(image, image, new System.Drawing.Size(200, 300), interpolation: Emgu.CV.CvEnum.Inter.Cubic);
                                image.Save(file);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.GetType() == typeof(ArgumentException)) MessageBox.Show($"Jeden ze souborů se nepodařilo načíst. Zkontrolujte zda jsou všechny v pořádku a zkuste to prosím znovu");
                        }
                    }
                },
                () =>
                {
                    return PreProcessed;
                });
            ChangePhoto = new RelayCommand(
                () =>
                {
                    OpenFileDialog dialog = new OpenFileDialog();
                    dialog.Multiselect = false;
                    dialog.DefaultExt = ".png";
                    dialog.Filter = "Image Files (*.BMP;*.JPG;*.PNG)|*.BMP;*.JPG;*.PNG|All files (*.*)|*.*";

                    if (dialog.ShowDialog() == true)
                    {
                        Image<Bgr, byte> colored = new Image<Bgr, byte>(dialog.FileName);
                        Image = Helper.ToBitmapSource(colored);
                    }
                    AddPerson.RaiseCanExecuteChanged();
                },
                () => true);
            AddPerson = new RelayCommand(
                () =>
                {
                    ImageNames person = new ImageNames()
                    {
                        Image = _img,
                        Name = _name
                    };
                    ImageNames.Add(person);
                },
                () => Name != "" && Image != null);
            RemovePerson = new RelayCommand(
                () =>
                {
                    ImageNames.Remove(SelectedImg);
                },
                () => SelectedImg != null);
            EditPerson = new RelayCommand(
                () =>
                {
                    SelectedImg.Name = Name;
                    SelectedImg.Image = Image;
                },
                () => SelectedImg != null);
        }
        public void SelectionChanged()
        {
            RemovePerson.RaiseCanExecuteChanged();
            EditPerson.RaiseCanExecuteChanged();
            if (SelectedImg == null) return;
            Name = SelectedImg.Name;
            Image = SelectedImg.Image;
        }
    }
}
