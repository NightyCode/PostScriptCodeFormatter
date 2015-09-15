namespace NightyCode.PostScript.CodeFormatter.ViewModels
{
    #region Namespace Imports

    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Text;
    using System.Windows.Input;

    using Microsoft.Practices.Prism.Commands;
    using Microsoft.Practices.Prism.Mvvm;

    using NightyCode.PostScript.Filters;

    #endregion


    public class DecodeStreamViewModel : BindableBase
    {
        #region Constants and Fields

        private readonly DelegateCommand _decodeCommand;
        private readonly DelegateCommand _saveCommand;

        private Stream _dataStream;
        private string _selectedFilter;
        private string _sourceStreamData;

        #endregion


        #region Constructors and Destructors

        public DecodeStreamViewModel()
        {
            _decodeCommand = new DelegateCommand(Decode, CanDecode);
            _saveCommand = new DelegateCommand(Save, CanSave);
        }

        #endregion


        #region Properties

        public Stream DataStream
        {
            get
            {
                return _dataStream;
            }

            private set
            {
                if (SetProperty(ref _dataStream, value))
                {
                    OnDataStreamChanged();
                }
            }
        }

        public ICommand DecodeCommand
        {
            get
            {
                return _decodeCommand;
            }
        }

        public IEnumerable<string> Filters
        {
            get
            {
                return Enum.GetNames(typeof(FilterType));
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                return _saveCommand;
            }
        }

        public string SelectedFilter
        {
            get
            {
                return _selectedFilter;
            }

            set
            {
                if (SetProperty(ref _selectedFilter, value))
                {
                    _decodeCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public string SourceStreamData
        {
            get
            {
                return _sourceStreamData;
            }
            set
            {
                if (!SetProperty(ref _sourceStreamData, value))
                {
                    return;
                }

                if (string.IsNullOrEmpty(_sourceStreamData))
                {
                    DataStream = null;
                }
                else
                {
                    var stream = new MemoryStream();

                    foreach (char c in _sourceStreamData)
                    {
                        if (char.IsWhiteSpace(c))
                        {
                            continue;
                        }

                        stream.WriteByte((byte)c);
                    }

                    stream.Seek(0, SeekOrigin.Begin);

                    DataStream = stream;
                }
            }
        }

        #endregion


        #region Methods

        private bool CanDecode()
        {
            FilterType result;
            return _sourceStreamData != null && Enum.TryParse(SelectedFilter, out result);
        }


        private bool CanSave()
        {
            return DataStream != null;
        }


        private void Decode()
        {
            if (!CanDecode())
            {
                return;
            }

            var filter = (FilterType)Enum.Parse(typeof(FilterType), SelectedFilter);

            Stream decodedStream = StreamFilter.Decode(DataStream, filter);

            if (decodedStream == null)
            {
                _sourceStreamData = "Error decoding data";
                DataStream = null;
            }
            else
            {
                using (TextReader reader = new StreamReader(decodedStream, Encoding.Default, true, 256, true))
                {
                    _sourceStreamData = reader.ReadToEnd();
                }

                decodedStream.Seek(0, SeekOrigin.Begin);

                DataStream = decodedStream;
            }

            OnPropertyChanged(() => SourceStreamData);
        }


        private void OnDataStreamChanged()
        {
            _decodeCommand.RaiseCanExecuteChanged();
            _saveCommand.RaiseCanExecuteChanged();
        }


        private void Save()
        {
            if (!CanSave())
            {
                return;
            }

            using (var stream = new FileStream("DecodedStreamData.bin", FileMode.Create))
            {
                _dataStream.CopyTo(stream);
                _dataStream.Seek(0, SeekOrigin.Begin);
            }
        }


        private void SaveAsBitmap(int width, int height, PixelFormat format, string fileName)
        {
            var bitmap = new Bitmap(width, height, format);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    int r = _dataStream.ReadByte();
                    int g = _dataStream.ReadByte();
                    int b = _dataStream.ReadByte();

                    if (r == -1 || g == -1 || b == -1)
                    {
                        throw new Exception("Invalid stream size");
                    }

                    bitmap.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            bitmap.Save(fileName);
            bitmap.Dispose();

            _dataStream.Seek(0, SeekOrigin.Begin);
        }

        #endregion
    }
}