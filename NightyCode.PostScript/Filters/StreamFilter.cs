namespace NightyCode.PostScript.Filters
{
    #region Namespace Imports

    using System;
    using System.IO;

    #endregion


    public static class StreamFilter
    {
        #region Public Methods

        public static Stream Decode(Stream sourceStream, FilterType filterType)
        {
            Stream decodedStream;

            switch (filterType)
            {
                case FilterType.Ascii85Decode:
                    decodedStream = DecodeAscii85Stream(sourceStream);
                    break;

                case FilterType.RunLengthDecode:
                    decodedStream = DecodeRunLengthStream(sourceStream);
                    break;

                default:
                    throw new NotImplementedException();
            }

            decodedStream?.Seek(0, SeekOrigin.Begin);

            return decodedStream;
        }

        #endregion


        #region Methods

        private static Stream DecodeAscii85Stream(Stream sourceStream)
        {
            var memoryStream = new MemoryStream();

            var inputBuffer = new byte[5];
            var outputBuffer = new byte[4];

            while (true)
            {
                var bytesRead = 0;
                var isEndOfStream = false;
                var tildeRead = false;

                while (bytesRead < 5)
                {
                    int readByte = sourceStream.ReadByte();

                    if (readByte == -1)
                    {
                        return memoryStream;
                    }

                    if (readByte >= '!' && readByte <= 'u')
                    {
                        if (tildeRead == false || readByte != '>')
                        {
                            inputBuffer[bytesRead] = (byte)readByte;
                            bytesRead++;
                        }
                    }
                    else if (readByte == 'z')
                    {
                        inputBuffer[0] = (byte)'!';
                        inputBuffer[1] = (byte)'!';
                        inputBuffer[2] = (byte)'!';
                        inputBuffer[3] = (byte)'!';
                        inputBuffer[4] = (byte)'!';

                        bytesRead = 5;
                    }
                    else if (readByte == '~')
                    {
                        if (tildeRead)
                        {
                            return null;
                        }

                        tildeRead = true;

                        continue;
                    }
                    else
                    {
                        return null;
                    }

                    if (!tildeRead)
                    {
                        continue;
                    }

                    if (readByte != '>')
                    {
                        return null;
                    }

                    for (int i = bytesRead; i < 5; i++)
                    {
                        inputBuffer[i] = 0x21 + 84;
                    }

                    isEndOfStream = true;

                    break;
                }

                ulong value = ((ulong)(inputBuffer[0] - 33) * 52200625) + ((ulong)(inputBuffer[1] - 33) * 614125)
                              + ((ulong)(inputBuffer[2] - 33) * 7225) + ((ulong)(inputBuffer[3] - 33) * 85)
                              + ((ulong)(inputBuffer[4] - 33) * 1);

                outputBuffer[0] = (byte)((value >> 24) & 0xFF);
                outputBuffer[1] = (byte)((value >> 16) & 0xFF);
                outputBuffer[2] = (byte)((value >> 8) & 0xFF);
                outputBuffer[3] = (byte)((value >> 0) & 0xFF);

                int outputByteCount = bytesRead < 5 ? bytesRead : 4;

                if (outputByteCount > 0)
                {
                    memoryStream.Write(outputBuffer, 0, outputByteCount);
                }

                if (isEndOfStream)
                {
                    return memoryStream;
                }
            }
        }


        private static Stream DecodeRunLengthStream(Stream sourceStream)
        {
            var memoryStream = new MemoryStream();

            while (true)
            {
                int length = sourceStream.ReadByte();

                if (length == -1 || length == 128)
                {
                    return memoryStream;
                }

                if (length <= 127)
                {
                    for (var i = 0; i < length + 1; i++)
                    {
                        int value = sourceStream.ReadByte();

                        if (value == -1)
                        {
                            return null;
                        }

                        memoryStream.WriteByte((byte)value);
                    }
                }
                else
                {
                    int value = sourceStream.ReadByte();

                    if (value == -1)
                    {
                        return null;
                    }

                    for (var i = 0; i < 257 - length; i++)
                    {
                        memoryStream.WriteByte((byte)value);
                    }
                }
            }
        }

        #endregion
    }
}