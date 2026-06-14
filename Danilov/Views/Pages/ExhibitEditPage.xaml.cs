using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class ExhibitEditPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private int editId = -1;
        private List<string> photoPaths = new List<string>();
        private int selectedThumbnailIndex = -1;
        private int originalDataVersion = 0;
        public event EventHandler ExhibitSaved;

        public ExhibitEditPage(DatabaseService dbService, User currentUser, int id = -1)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            this.editId = id;

            LoadResponsiblePersons();
            LoadLocations();

            if (id == -1)
            {
                lblTitle.Text = "Добавление экспоната";
            }
            else
            {
                lblTitle.Text = "Редактирование экспоната";
                LoadData();
            }
        }

        private void LoadLocations()
        {
            cmbLocation.Items.Clear();
            var locations = dbService.GetLocations();
            if (locations != null && locations.Count > 0)
            {
                foreach (var location in locations)
                {
                    cmbLocation.Items.Add(new ComboBoxItem { Content = location });
                }
            }
            cmbLocation.SelectedIndex = 0;
        }

        private void LoadResponsiblePersons()
        {
            cmbResponsiblePerson.Items.Clear();
            var teachers = dbService.GetAllTeachers();
            if (teachers != null && teachers.Count > 0)
            {
                foreach (var teacher in teachers)
                {
                    cmbResponsiblePerson.Items.Add(new ComboBoxItem { Content = teacher.FullName });
                }
            }
            
        }

        private void LoadData()
        {
            var exhibits = dbService.GetAllExhibits();
            var ex = exhibits.Find(x => x.Id == editId);

            if (ex != null)
            {
                originalDataVersion = ex.DataVersion;
                txtInventoryNumber.Text = ex.InventoryNumber;
                txtName.Text = ex.Name;
                txtCategory.Text = ex.Category;
                txtMaterial.Text = ex.Material;

                if (ex.Condition == "В наличии")
                    cmbCondition.SelectedIndex = 0;
                else if (ex.Condition == "Выдан")
                    cmbCondition.SelectedIndex = 1;
                else
                    cmbCondition.SelectedIndex = 0;

                string locationName = ex.Location;
                if (ex.LocationId.HasValue)
                {
                    string resolvedLocation = dbService.GetLocationNameById(ex.LocationId.Value);
                    if (!string.IsNullOrEmpty(resolvedLocation))
                    {
                        locationName = resolvedLocation;
                    }
                }

                if (!string.IsNullOrEmpty(locationName))
                {
                    bool found = false;
                    for (int i = 0; i < cmbLocation.Items.Count; i++)
                    {
                        ComboBoxItem item = cmbLocation.Items[i] as ComboBoxItem;
                        if (item != null && item.Content.ToString() == locationName)
                        {
                            cmbLocation.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        cmbLocation.SelectedIndex = 0;
                }
                else
                {
                    cmbLocation.SelectedIndex = 0;
                }

                txtCost.Text = ex.Cost.ToString();

                if (ex.YearOfOrigin.HasValue)
                    txtYearOfOrigin.Text = ex.YearOfOrigin.Value.ToString();

                if (ex.LastRestorationDate.HasValue)
                    dpLastRestoration.SelectedDate = ex.LastRestorationDate.Value;

                if (!string.IsNullOrEmpty(ex.ResponsiblePerson))
                {
                    bool found = false;
                    for (int i = 0; i < cmbResponsiblePerson.Items.Count; i++)
                    {
                        ComboBoxItem item = cmbResponsiblePerson.Items[i] as ComboBoxItem;
                        if (item != null && item.Content.ToString() == ex.ResponsiblePerson)
                        {
                            cmbResponsiblePerson.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        cmbResponsiblePerson.Text = ex.ResponsiblePerson;
                }

                if (!string.IsNullOrEmpty(ex.Source))
                {
                    bool found = false;
                    for (int i = 0; i < cmbSource.Items.Count; i++)
                    {
                        ComboBoxItem item = cmbSource.Items[i] as ComboBoxItem;
                        if (item != null && item.Content.ToString() == ex.Source)
                        {
                            cmbSource.SelectedIndex = i;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        cmbSource.Text = ex.Source;
                }

                if (ex.PhotoPaths != null && ex.PhotoPaths.Count > 0)
                {
                    foreach (var path in ex.PhotoPaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            photoPaths.Add(path);
                        }
                    }
                }
                else if (!string.IsNullOrEmpty(ex.PhotoPath) && File.Exists(ex.PhotoPath))
                {
                    photoPaths.Add(ex.PhotoPath);
                }

                UpdatePhotoDisplay();
            }
        }

        private void UpdatePhotoDisplay()
        {
            if (photoPaths.Count > 0)
            {
                txtNoPhoto.Visibility = Visibility.Collapsed;
                lstPhotoThumbnails.ItemsSource = null;
                lstPhotoThumbnails.ItemsSource = photoPaths;
                if (selectedThumbnailIndex >= 0 && selectedThumbnailIndex < photoPaths.Count)
                {
                    ShowPhoto(selectedThumbnailIndex);
                }
                else
                {
                    ShowPhoto(0);
                }
            }
            else
            {
                imgPhoto.Source = null;
                lstPhotoThumbnails.ItemsSource = null;
                txtNoPhoto.Visibility = Visibility.Visible;
            }
        }

        private void ShowPhoto(int index)
        {
            if (index < 0 || index >= photoPaths.Count)
                return;

            selectedThumbnailIndex = index;
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
                    imgPhoto.Source = bitmap;
                }
                else
                {
                    imgPhoto.Source = null;
                }
            }
            catch
            {
                imgPhoto.Source = null;
            }
        }

        private void BtnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";
            dialog.Multiselect = true;

            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileNames != null && dialog.FileNames.Length > 0)
                {
                    foreach (var file in dialog.FileNames)
                    {
                        if (!photoPaths.Contains(file))
                        {
                            photoPaths.Add(file);
                        }
                    }
                    UpdatePhotoDisplay();
                }
            }
        }

        private void BtnRemovePhoto_Click(object sender, RoutedEventArgs e)
        {
            if (selectedThumbnailIndex >= 0 && selectedThumbnailIndex < photoPaths.Count)
            {
                photoPaths.RemoveAt(selectedThumbnailIndex);
                if (photoPaths.Count > 0)
                {
                    selectedThumbnailIndex = Math.Min(selectedThumbnailIndex, photoPaths.Count - 1);
                }
                else
                {
                    selectedThumbnailIndex = -1;
                }
                UpdatePhotoDisplay();
            }
        }

        private void BtnClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            photoPaths.Clear();
            selectedThumbnailIndex = -1;
            UpdatePhotoDisplay();
        }

        private void LstPhotoThumbnails_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstPhotoThumbnails.SelectedIndex >= 0)
            {
                ShowPhoto(lstPhotoThumbnails.SelectedIndex);
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string inventoryNumber = txtInventoryNumber.Text.Trim();
            if (string.IsNullOrEmpty(inventoryNumber))
            {
                MessageBox.Show("Введите инвентарный номер", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtInventoryNumber.Focus();
                return;
            }

            if (dbService.IsInventoryNumberExists(inventoryNumber, editId == -1 ? null : (int?)editId))
            {
                MessageBox.Show("Экспонат с таким инвентарным номером уже существует!", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtInventoryNumber.Focus();
                return;
            }

            if (editId != -1)
            {
                int currentVersion = dbService.GetExhibitVersion(editId);
                if (currentVersion != originalDataVersion)
                {
                    MessageBox.Show("Данные были изменены другим пользователем. Пожалуйста, обновите страницу и попробуйте снова.", "Конфликт данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите название экспоната", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtName.Focus();
                return;
            }

            decimal cost = 0;
            if (!string.IsNullOrEmpty(txtCost.Text) && !decimal.TryParse(txtCost.Text, out cost))
            {
                MessageBox.Show("Введите корректную стоимость (число)", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCost.Focus();
                return;
            }

            int year = 0;
            if (!string.IsNullOrEmpty(txtYearOfOrigin.Text) && !int.TryParse(txtYearOfOrigin.Text, out year))
            {
                MessageBox.Show("Введите корректный год создания (число)", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtYearOfOrigin.Focus();
                return;
            }

            if (year < 0 || year > DateTime.Now.Year)
            {
                MessageBox.Show($"Год создания должен быть от 0 до {DateTime.Now.Year}", "Ошибка валидации", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtYearOfOrigin.Focus();
                return;
            }

            List<string> copiedPhotoPaths = new List<string>();
            string photosFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
            if (!Directory.Exists(photosFolder)) Directory.CreateDirectory(photosFolder);

            foreach (var originalPath in photoPaths)
            {
                if (!string.IsNullOrEmpty(originalPath) && File.Exists(originalPath))
                {
                    string normalizedPath = Path.GetFullPath(originalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string normalizedPhotosFolder = Path.GetFullPath(photosFolder).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    bool isAlreadyInPhotosFolder = normalizedPath.StartsWith(normalizedPhotosFolder + Path.DirectorySeparatorChar) || 
                                                  normalizedPath.StartsWith(normalizedPhotosFolder + Path.AltDirectorySeparatorChar);

                    if (isAlreadyInPhotosFolder)
                    {
                        copiedPhotoPaths.Add(originalPath);
                    }
                    else
                    {
                        string fileName = $"{txtInventoryNumber.Text}_{DateTime.Now.Ticks}_{Guid.NewGuid()}{Path.GetExtension(originalPath)}";
                        string destPath = Path.Combine(photosFolder, fileName);
                        File.Copy(originalPath, destPath, true);
                        copiedPhotoPaths.Add(destPath);
                    }
                }
            }

            Exhibit exhibit = new Exhibit();
            exhibit.Id = editId;
            exhibit.InventoryNumber = inventoryNumber;
            exhibit.Name = name;
            exhibit.Category = txtCategory.Text.Trim();
            exhibit.Material = txtMaterial.Text.Trim();

            ComboBoxItem conditionItem = cmbCondition.SelectedItem as ComboBoxItem;
            exhibit.Condition = conditionItem != null ? conditionItem.Content.ToString() : "В наличии";

            ComboBoxItem locationItem = cmbLocation.SelectedItem as ComboBoxItem;
            string location = locationItem != null ? locationItem.Content.ToString() : "Музей";
            if (cmbLocation.IsEditable && !string.IsNullOrEmpty(cmbLocation.Text) && 
                cmbLocation.Items.Cast<ComboBoxItem>().All(i => i.Content.ToString() != cmbLocation.Text))
            {
                location = cmbLocation.Text;
            }
            exhibit.Location = location;
            exhibit.LocationId = dbService.GetLocationIdByName(location);

            exhibit.PhotoPaths = copiedPhotoPaths;

            exhibit.Cost = cost;

            if (year > 0)
                exhibit.YearOfOrigin = year;

            if (dpLastRestoration.SelectedDate.HasValue)
                exhibit.LastRestorationDate = dpLastRestoration.SelectedDate.Value;

            ComboBoxItem responsibleItem = cmbResponsiblePerson.SelectedItem as ComboBoxItem;
            exhibit.ResponsiblePerson = responsibleItem != null ? responsibleItem.Content.ToString() : cmbResponsiblePerson.Text;

            ComboBoxItem sourceItem = cmbSource.SelectedItem as ComboBoxItem;
            exhibit.Source = sourceItem != null ? sourceItem.Content.ToString() : cmbSource.Text;

            await System.Threading.Tasks.Task.Run(() =>
            {
                if (editId == -1)
                    dbService.AddExhibit(exhibit, currentUser);
                else
                    dbService.UpdateExhibit(exhibit, currentUser);
            });

            MessageBox.Show("Экспонат сохранен");
            ExhibitSaved?.Invoke(this, EventArgs.Empty);
            NavigationService.GoBack();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
