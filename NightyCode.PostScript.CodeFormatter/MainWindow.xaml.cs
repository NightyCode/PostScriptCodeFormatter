namespace NightyCode.PostScript.CodeFormatter
{
    #region Namespace Imports

    using System.Windows;

    #endregion


    public partial class MainWindow
    {
        private readonly PostScriptFormatter _formatter;


        public MainWindow()
        {
            InitializeComponent();

            _formatter = new PostScriptFormatter();
        }


        private void OnFormatButtonClick(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Text = string.Empty;

            if (string.IsNullOrWhiteSpace(SourceTextBox.Text))
            {
                return;
            }

            ResultTextBox.Text = _formatter.Format(SourceTextBox.Text);
        }
    }
}