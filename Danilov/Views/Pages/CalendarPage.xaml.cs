using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MuseumAccountingSystem.Models;
using MuseumAccountingSystem.Services;

namespace MuseumAccountingSystem.Views.Pages
{
    public partial class CalendarPage : Page
    {
        private DatabaseService dbService;
        private DateTime currentDate;
        private List<Issue> activeIssues;

        public CalendarPage(DatabaseService dbService)
        {
            InitializeComponent();
            this.dbService = dbService;
            currentDate = DateTime.Now;
            dbService.DataChanged += OnDataChanged;
            LoadCalendar();
        }

        private void OnDataChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() => LoadCalendar()));
        }

        private void LoadCalendar()
        {
            activeIssues = dbService.GetAllIssues(true);
            txtCurrentMonth.Text = currentDate.ToString("MMMM yyyy");

            var firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
            var daysInMonth = DateTime.DaysInMonth(currentDate.Year, currentDate.Month);

            var startDate = firstDayOfMonth.AddDays(-(int)firstDayOfMonth.DayOfWeek);

            var calendarDays = new List<CalendarDay>();

            for (int i = 0; i < 42; i++)
            {
                var date = startDate.AddDays(i);
                var isCurrentMonth = date.Month == currentDate.Month;

                var dayEvents = activeIssues
                    .Where(issue => issue.PlannedReturnDate.Date == date.Date)
                    .Select(issue => new CalendarEvent
                    {
                        ExhibitName = issue.ExhibitName,
                        ExhibitInventoryNumber = issue.ExhibitInventoryNumber,
                        TeacherName = issue.TeacherName,
                        IssueId = issue.Id
                    })
                    .ToList();

                calendarDays.Add(new CalendarDay
                {
                    Date = date,
                    Day = date.Day.ToString(),
                    IsCurrentMonth = isCurrentMonth,
                    Events = dayEvents,
                    Background = isCurrentMonth ? (date.Date == DateTime.Now.Date ? "#6C3483" : "#2D2D2D") : "#1E1E1E",
                    DayColor = isCurrentMonth ? (date.Date == DateTime.Now.Date ? "#FFFFFF" : "#E0E0E0") : "#555555"
                });
            }

            calendarItems.ItemsSource = calendarDays;
        }

        private void BtnPrevMonth_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddMonths(-1);
            LoadCalendar();
        }

        private void BtnNextMonth_Click(object sender, RoutedEventArgs e)
        {
            currentDate = currentDate.AddMonths(1);
            LoadCalendar();
        }

        private void BtnToday_Click(object sender, RoutedEventArgs e)
        {
            currentDate = DateTime.Now;
            LoadCalendar();
        }

        private void Event_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var border = sender as Border;
            var calendarEvent = border?.DataContext as CalendarEvent;

            if (calendarEvent != null)
            {
                MessageBox.Show($"Экспонат: {calendarEvent.ExhibitName}\nИнв. номер: {calendarEvent.ExhibitInventoryNumber}\nПреподаватель: {calendarEvent.TeacherName}",
                    "Информация о возврате",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }

    public class CalendarDay
    {
        public DateTime Date { get; set; }
        public string Day { get; set; }
        public bool IsCurrentMonth { get; set; }
        public List<CalendarEvent> Events { get; set; }
        public string Background { get; set; }
        public string DayColor { get; set; }
    }

    public class CalendarEvent
    {
        public string ExhibitName { get; set; }
        public string ExhibitInventoryNumber { get; set; }
        public string TeacherName { get; set; }
        public int IssueId { get; set; }
    }
}