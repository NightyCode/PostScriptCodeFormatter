namespace NightyCode.PostScript.CodeFormatter
{
    #region Namespace Imports

    using System.Windows.Input;

    using Microsoft.Practices.Prism.Commands;
    using Microsoft.Practices.Prism.Mvvm;

    #endregion


    public class MainViewModel : BindableBase
    {
        #region Constants and Fields

        private readonly PostScriptFormatter _formatter;
        private readonly DelegateCommand _processCodeCommand;
        private string _processedCode;
        private string _sourceCode;

        #endregion


        #region Constructors and Destructors

        public MainViewModel()
        {
            _formatter = new PostScriptFormatter();

            _processCodeCommand = new DelegateCommand(ProcessCode);
        }

        #endregion


        #region Properties

        public bool FormatCode
        {
            get
            {
                return _formatter.FormatCode;
            }
            set
            {
                _formatter.FormatCode = value;
            }
        }

        public bool AddTracing
        {
            get
            {
                return _formatter.AddTracing;
            }
            set
            {
                _formatter.AddTracing = value;
            }
        }

        public string ProcessedCode
        {
            get
            {
                return _processedCode;
            }
            set
            {
                SetProperty(ref _processedCode, value);
            }
        }

        public ICommand ProcessCodeCommand
        {
            get
            {
                return _processCodeCommand;
            }
        }

        public bool RemoveOperatorAliases
        {
            get
            {
                return _formatter.RemoveOperatorAliases;
            }
            set
            {
                _formatter.RemoveOperatorAliases = value;
            }
        }

        public string SourceCode
        {
            get
            {
                return _sourceCode;
            }
            set
            {
                SetProperty(ref _sourceCode, value);
            }
        }

        #endregion


        #region Methods

        private void ProcessCode()
        {
            if (string.IsNullOrWhiteSpace(SourceCode))
            {
                return;
            }

            _formatter.FormatCode = FormatCode;
            _formatter.RemoveOperatorAliases = RemoveOperatorAliases;

            ProcessedCode = _formatter.Format(SourceCode);
        }

        #endregion
    }
}