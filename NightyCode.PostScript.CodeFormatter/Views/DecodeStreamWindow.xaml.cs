namespace NightyCode.PostScript.CodeFormatter.Views
{
    #region Namespace Imports

    using System.Windows;

    using NightyCode.PostScript.CodeFormatter.ViewModels;

    #endregion


    /// <summary>
    ///     Interaction logic for DecodeStreamWindow.xaml
    /// </summary>
    public partial class DecodeStreamWindow : Window
    {
        #region Constructors and Destructors

        public DecodeStreamWindow()
        {
            InitializeComponent();

            DataContext = new DecodeStreamViewModel();
        }

        #endregion
    }
}