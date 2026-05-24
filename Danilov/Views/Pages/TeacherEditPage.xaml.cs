using System;
using System.Windows;
using System.Windows.Controls;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class TeacherEditPage : Page
    {
        private DatabaseService dbService;
        private User currentUser;
        private int editId = -1;
        public event EventHandler TeacherSaved;

        public TeacherEditPage(DatabaseService dbService, User currentUser, int id = -1)
        {
            InitializeComponent();
            this.dbService = dbService;
            this.currentUser = currentUser;
            this.editId = id;

            if (id == -1)
            {
                lblTitle.Text = "Добавление преподавателя";
            }
            else
            {
                lblTitle.Text = "Редактирование преподавателя";
                LoadData();
            }
        }

        private void LoadData()
        {
            var teachers = dbService.GetAllTeachers();
            var teacher = teachers.Find(x => x.Id == editId);

            if (teacher != null)
            {
                txtFullName.Text = teacher.FullName;
                txtDepartment.Text = teacher.Department;
                txtEmail.Text = teacher.Email;
                txtPhone.Text = teacher.Phone;
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtFullName.Text))
            {
                MessageBox.Show("Введите ФИО преподавателя");
                return;
            }

            Teacher teacher = new Teacher();
            teacher.Id = editId;
            teacher.FullName = txtFullName.Text;
            teacher.Department = txtDepartment.Text;
            teacher.Email = txtEmail.Text;
            teacher.Phone = txtPhone.Text;

            try
            {
                await System.Threading.Tasks.Task.Run(() =>
                {
                    if (editId == -1)
                        dbService.AddTeacher(teacher, currentUser);
                    else
                        dbService.UpdateTeacher(teacher, currentUser);
                });

                MessageBox.Show("Преподаватель сохранен");
                TeacherSaved?.Invoke(this, EventArgs.Empty);
                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}
