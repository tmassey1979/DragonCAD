using Avalonia.Controls;

namespace DragonCAD.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainWindowViewModel viewModel = MainWindowViewModel.CreateDesignPreview();
        viewModel.ApplyStartupTab(Environment.GetEnvironmentVariable("DRAGONCAD_START_TAB"));
        DataContext = viewModel;
    }
}
