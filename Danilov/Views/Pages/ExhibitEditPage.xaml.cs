using System;
using System.IO;
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
        private int editId = -1;
        private string photoPath = "";
        public event EventHandler ExhibitSaved;

        public ExhibitEditPage(DatabaseService dbService, int id = -1)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.editId = id;

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

        private void LoadData()
        {
            var exhibits = dbService.GetAllExhibits();
            var ex = exhibits.Find(x => x.Id == editId);

            if (ex != null)
            {
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

                if (!string.IsNullOrEmpty(ex.Location))
                {
                    bool found = false;
                    for (int i = 0; i < cmbLocation.Items.Count; i++)
                    {
                        ComboBoxItem item = cmbLocation.Items[i] as ComboBoxItem;
                        if (item != null && item.Content.ToString() == ex.Location)
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

                if (!string.IsNullOrEmpty(ex.PhotoPath) && File.Exists(ex.PhotoPath))
                {
                    photoPath = ex.PhotoPath;
                    imgPhoto.Source = new BitmapImage(new Uri(ex.PhotoPath));
                }
            }
        }

        private void BtnLoadPhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp";

            if (dialog.ShowDialog() == true)
            {
                photoPath = dialog.FileName;
                imgPhoto.Source = new BitmapImage(new Uri(photoPath));
            }
        }

        private void BtnClearPhoto_Click(object sender, RoutedEventArgs e)
        {
            photoPath = "";
            imgPhoto.Source = null;
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtInventoryNumber.Text))
            {
                MessageBox.Show("Введите инвентарный номер");
                return;
            }

            if (string.IsNullOrEmpty(txtName.Text))
            {
                MessageBox.Show("Введите название");
                return;
            }

            string finalPhoto = "";
            if (!string.IsNullOrEmpty(photoPath))
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Photos");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string fileName = $"{txtInventoryNumber.Text}_{DateTime.Now.Ticks}{Path.GetExtension(photoPath)}";
                finalPhoto = Path.Combine(folder, fileName);
                File.Copy(photoPath, finalPhoto, true);
            }

            Exhibit exhibit = new Exhibit();
            exhibit.Id = editId;
            exhibit.InventoryNumber = txtInventoryNumber.Text;
            exhibit.Name = txtName.Text;
            exhibit.Category = txtCategory.Text;
            exhibit.Material = txtMaterial.Text;

            ComboBoxItem conditionItem = cmbCondition.SelectedItem as ComboBoxItem;
            exhibit.Condition = conditionItem != null ? conditionItem.Content.ToString() : "В наличии";

            ComboBoxItem locationItem = cmbLocation.SelectedItem as ComboBoxItem;
            exhibit.Location = locationItem != null ? locationItem.Content.ToString() : "Музей";

            exhibit.PhotoPath = finalPhoto;

            await System.Threading.Tasks.Task.Run(() =>
            {
                if (editId == -1)
                    dbService.AddExhibit(exhibit);
                else
                    dbService.UpdateExhibit(exhibit);
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