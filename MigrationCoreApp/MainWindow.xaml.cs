using System.Windows.Controls.Ribbon;
using MigrationCoreApp.ViewModels;

namespace MigrationCoreApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow(MainWindowViewModel model)
        {
            InitializeComponent();

            DataContext = model;
        }
    }
}
