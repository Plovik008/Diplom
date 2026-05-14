using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MuseumAccountingSystem.Views
{
    public partial class PhotoViewerWindow : Window
    {
        private List<string> photoPaths;
        private int currentIndex = 0;

        public PhotoViewerWindow(string exhibitName, List<string> photos)
        {
            InitializeComponent();
            txtTitle.Text = $"Фотографии: {exhibitName}";
            photoPaths = photos;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (photoPaths == null || photoPaths.Count == 0)
            {
                MessageBox.Show("Нет фотографий для отображения", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            lstPhotos.ItemsSource = photoPaths;
            ShowPhoto(0);
        }

        private void ShowPhoto(int index)
        {
            if (index < 0 || index >= photoPaths.Count)
                return;

            currentIndex = index;

            try
            {
                string path = photoPaths[index];
                if (File.Exists(path))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(path);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    imgMain.Source = bitmap;
                }
                else
                {
                    imgMain.Source = null;
                }
            }
            catch
            {
                imgMain.Source = null;
            }

            txtCounter.Text = $"{currentIndex + 1} из {photoPaths.Count}";
            lstPhotos.SelectedIndex = currentIndex;
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex > 0)
                ShowPhoto(currentIndex - 1);
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (currentIndex < photoPaths.Count - 1)
                ShowPhoto(currentIndex + 1);
        }

        private void LstPhotos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPhotos.SelectedIndex >= 0)
                ShowPhoto(lstPhotos.SelectedIndex);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}