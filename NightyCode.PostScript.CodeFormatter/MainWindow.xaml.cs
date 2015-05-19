namespace NightyCode.PostScript.CodeFormatter
{
    #region Namespace Imports

    

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
    }
}