namespace NightyCode.PostScript.CodeFormatter.ViewModels
{
    #region Namespace Imports

    using System.IO;
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
        private string _selectedFileName;
        private string _sourceCode;

        #endregion


        #region Constructors and Destructors

        public MainViewModel()
        {
            _formatter = new PostScriptFormatter();

            _processCodeCommand = new DelegateCommand(ProcessCode, CanProcessCode);
        }

        #endregion


        #region Properties

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

        public ICommand ProcessCodeCommand
        {
            get
            {
                return _processCodeCommand;
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

        public string SelectedFileName
        {
            get
            {
                return _selectedFileName;
            }

            set
            {
                if (SetProperty(ref _selectedFileName, value))
                {
                    OnSelectedFileNameChanged();
                }
            }
        }

        public string SourceCode
        {
            get
            {
                return _sourceCode;
            }

            private set
            {
                if (SetProperty(ref _sourceCode, value))
                {
                    _processCodeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        #endregion


        #region Methods

        private bool CanProcessCode()
        {
            return !string.IsNullOrEmpty(SourceCode);
        }


        private void OnSelectedFileNameChanged()
        {
            if (string.IsNullOrEmpty(_selectedFileName) || !File.Exists(_selectedFileName))
            {
                SourceCode = null;

                return;
            }

            SourceCode = File.ReadAllText(_selectedFileName);
        }


        private async void ProcessCode()
        {
            if (!CanProcessCode())
            {
                return;
            }

            var fileStream = new FileStream(SelectedFileName, FileMode.Open);

            ProcessedCode = await _formatter.Format(fileStream).ConfigureAwait(true);
        }

        #endregion
    }
}