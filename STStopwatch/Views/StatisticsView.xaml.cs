using System.Windows;
using System.Windows.Controls;
using STStopwatch.ViewModels;

namespace STStopwatch.Views
{
    public partial class StatisticsView : UserControl
    {
        public StatisticsView()
        {
            InitializeComponent();
        }

        private void OnDayViewTabClicked(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.StatisticsVM != null)
            {
                vm.StatisticsVM.SelectedViewIndex = 0;
            }
        }

        private void OnMonthViewTabClicked(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as MainViewModel;
            if (vm != null && vm.StatisticsVM != null)
            {
                vm.StatisticsVM.SelectedViewIndex = 1;
            }
        }
    }
}
