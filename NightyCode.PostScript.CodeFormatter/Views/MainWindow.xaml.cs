namespace NightyCode.PostScript.CodeFormatter.Views
{
    #region Namespace Imports

    using System.Windows;

    using Microsoft.Win32;

    using NightyCode.PostScript.CodeFormatter.ViewModels;

    #endregion


    public partial class MainWindow
    {
        #region Constructors and Destructors

        public MainWindow()
        {
            InitializeComponent();

            // TODO: Use prism.
            DataContext = new MainViewModel();
        }

        #endregion


        #region Methods

        private void OnDecodeStreamMenuItemClick(object sender, RoutedEventArgs e)
        {
            var window = new DecodeStreamWindow();
            window.ShowDialog();
        }


        private void OnExitMenuItemClick(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void OnOpenMenuItemClick(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Title = "Open Postscript File",
                Filter = "Postscript Files|*.ps|All Files|*.*"
            };

            bool? result = openFileDialog.ShowDialog(this);

            if (result.GetValueOrDefault())
            {
                FileNameTextBlock.Text = openFileDialog.FileName;
            }
        }

        #endregion
    }
}