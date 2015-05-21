namespace NightyCode.PostScript
{
    #region Namespace Imports

    using System;

    #endregion


    /// <summary>
    ///     Radix is a convertor class for converting numbers to different radices
    ///     e.g. display the number 1000 in base 16
    /// </summary>
    public class Radix
    {
        #region Constants and Fields

        private static readonly string _digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private static readonly string _radixDecodeErrorMessage = "RadixError: generic decode error.";
        private static readonly string _radixFormatErrorMessage = "RadixError: number not in radix format.";
        private static readonly string _radixNoSymbolFormatErrorMessage = "RadixError: number not in symbolic format.";
        private static readonly string _radixTooLargeErrorMessage1 = "RadixError: radix larger than 36.";
        private static readonly string _radixTooLargeErrorMessage2 = "RadixError: radix larger than 1000000.";
        private static readonly string _radixTooSmallErrorMessage = "RadixError: radix smaller than 2.";

        #endregion


        #region Public Methods

        public static void Decode(string val, long radix, out long rv)
        {
            Decode(val, radix, out rv, false);
        }


        /// <summary>
        ///     Decoder for a string to a long with the base [radix]. if sym is true
        ///     the number will be converted from a generic symbolic notation.
        /// </summary>
        public static void Decode(string val, long radix, out long rv, bool sym)
        {
            CheckArg(radix, sym);
            rv = 0;
            try
            {
                if (sym)
                {
                    string ws = val.Trim();
                    if (ws[0] != '[')
                    {
                        throw new Exception(_radixNoSymbolFormatErrorMessage);
                    }
                    // strip [(
                    ws = ws.Substring(2);
                    // get radix
                    int pos = ws.IndexOf(')');
                    long tr = long.Parse(ws.Substring(0, pos));
                    // strip it
                    ws = ws.Substring(pos + 2);
                    ws = ws.Remove(ws.Length - 1, 1); // strip ]

                    char sign = ws[0];
                    var si = 1;
                    if ((sign == '-') || (sign == '+'))
                    {
                        if (sign == '-')
                        {
                            si = -1;
                        }
                        ws = ws.Substring(2); // skip sign and ,
                    }

                    string[] t = ws.Split(',');
                    for (var i = 0; i < t.Length; i++)
                    {
                        rv *= radix;
                        long l = long.Parse(t[i]);
                        if (l >= radix)
                        {
                            throw new Exception(_radixFormatErrorMessage);
                        }
                        rv += l;
                    }
                    // add sign
                    rv *= si;
                }
                else
                {
                    string ws = val.Trim();
                    char sign = ws[0];
                    var si = 1;
                    if ((sign == '-') || (sign == '+'))
                    {
                        if (sign == '-')
                        {
                            si = -1;
                        }
                        ws = ws.Substring(1);
                    }

                    for (var i = 0; i < ws.Length; i++)
                    {
                        rv *= radix;
                        char c = ws[i];
                        long l = _digits.IndexOf(c);
                        if (l >= radix)
                        {
                            throw new Exception(_radixFormatErrorMessage);
                        }
                        rv += l;
                    }
                    // add sign
                    rv *= si;
                }
            }
            catch
            {
                throw new Exception(_radixDecodeErrorMessage);
            }
        }


        public static void Decode(string val, long radix, out double rv)
        {
            Decode(val, radix, out rv, false);
        }


        public static void Decode(string val, long radix, out double rv, bool sym)
        {
            CheckArg(radix, sym);
            rv = 0;

            try
            {
                double tradix = 1;
                if (sym)
                {
                    string ws = val.Trim();
                    // strip [(
                    ws = ws.Substring(2);
                    // get radix
                    int pos = ws.IndexOf(')');
                    long tr = long.Parse(ws.Substring(0, pos));
                    // strip it
                    ws = ws.Substring(pos + 2);
                    ws = ws.Remove(ws.Length - 1, 1); // strip ]

                    char sign = ws[0];
                    var si = 1;
                    if ((sign == '-') || (sign == '+'))
                    {
                        if (sign == '-')
                        {
                            si = -1;
                        }
                        ws = ws.Substring(2); // skip sign and ,
                    }

                    string[] t = ws.Split(',');
                    var before = true;
                    for (var i = 0; i < t.Length; i++)
                    {
                        if (t[i] == ".")
                        {
                            before = false;
                            continue;
                        }
                        // next 'digit'
                        long l = long.Parse(t[i]);
                        if (l >= radix)
                        {
                            throw new Exception(_radixFormatErrorMessage);
                        }

                        if (before)
                        {
                            // process before dec. point
                            rv *= radix;
                            rv += l;
                        }
                        else
                        {
                            // process after decimal point
                            tradix *= radix;
                            rv += l / tradix;
                        }
                    }

                    // add sign
                    rv *= si;
                }
                else
                {
                    string ws = val.Trim();
                    char sign = ws[0];
                    var si = 1;
                    if ((sign == '-') || (sign == '+'))
                    {
                        if (sign == '-')
                        {
                            si = -1;
                        }
                        ws = ws.Substring(1);
                    }

                    var before = true;
                    for (var i = 0; i < ws.Length; i++)
                    {
                        if (ws[i] == '.')
                        {
                            before = false;
                            continue;
                        }
                        // next 'digit'
                        long l = _digits.IndexOf(ws[i]);
                        if (l >= radix)
                        {
                            throw new Exception(_radixFormatErrorMessage);
                        }

                        if (before)
                        {
                            // process before dec. point
                            rv *= radix;
                            rv += l;
                        }
                        else
                        {
                            // process after decimal point
                            tradix *= radix;
                            rv += _digits.IndexOf(ws[i]) / tradix;
                        }
                    }
                    // add sign
                    rv *= si;
                }
            }
            catch
            {
                throw new Exception(_radixDecodeErrorMessage);
            }
        }


        public static string Encode(long x, long radix)
        {
            return Encode(x, radix, false);
        }


        /// <summary>
        ///     Encoder for a long to a string with the base [radix]. if sym is true
        ///     the number will be converted to a generic symbolic notation.
        /// </summary>
        public static string Encode(long x, long radix, bool sym)
        {
            // check parameters
            CheckArg(radix, sym);

            // work in positive domain
            long t = Math.Abs(x);

            // return value
            var rv = "";

            if (sym)
            {
                if (t == 0)
                {
                    rv = ",0";
                }
                while (t > 0)
                {
                    // split of one digit
                    long r = t % radix;
                    // convert it and add it to the return string
                    rv = "," + r + rv;
                    t = (t - r) / radix;
                }
                rv = rv.Substring(1); // strip one ','
                // add sign
                if (x < 0)
                {
                    rv = "-," + rv;
                }
                if (x == 0)
                {
                    rv = "0";
                }
                rv = "[(" + radix + ")," + rv + "]";
            }
            else
            {
                if (t == 0)
                {
                    rv = "0";
                }
                while (t > 0)
                {
                    // split of one digit
                    long r = t % radix;
                    // convert it and add it to the return string
                    rv = _digits[(int)r] + rv;
                    t = (t - r) / radix;
                }
                if (x < 0)
                {
                    // add sign
                    rv = "-" + rv;
                }
                if (x == 0)
                {
                    rv = "0";
                }
            }
            return rv;
        }


        public static string Encode(double x, long radix)
        {
            return Encode(x, radix, false);
        }


        public static string Encode(double x, long radix, bool sym)
        {
            // Check parameters
            CheckArg(radix, sym);

            double t = Math.Abs(x);

            // first part before decimal point
            var t1 = (long)t;

            // t2 holds part after decimal point
            double t2 = t - t1;

            // return value;
            var rv = "";

            if (sym)
            {
                if (x == 0.0)
                {
                    rv = ",0";
                }
                // process part before decimal point
                while (t1 > 0)
                {
                    long r = t1 % radix;
                    rv = "," + r + rv;
                    t1 = (t1 - r) / radix;
                }
                rv = rv.Substring(1); // strip one ','

                // after the decimal point
                if (t2 > 0.0)
                {
                    rv += ",.,";
                }
                var maxdigit = 50; // to prevent endless loop
                while (t2 > 0)
                {
                    var r = (long)(t2 * radix);
                    rv += r + ",";
                    t2 = (t2 * radix) - r;

                    // forced break after maxdigits
                    maxdigit--;
                    if (maxdigit == 0)
                    {
                        break;
                    }
                }
                rv = rv.Substring(0, rv.Length - 1); // strip one ','
                if (x < 0)
                {
                    rv = "-," + rv;
                }
                rv = "[(" + radix + ")," + rv + "]";
            }
            else
            {
                if (x == 0.0)
                {
                    rv = "0";
                }
                // process part before decimal point
                while (t1 > 0)
                {
                    long r = t1 % radix;
                    rv = _digits[(int)r] + rv;
                    t1 = (t1 - r) / radix;
                }

                // after the decimal point
                if (t2 > 0.0)
                {
                    rv += ".";
                }
                var maxdigit = 50; // to prevent endless loop
                while (t2 > 0)
                {
                    var r = (long)(t2 * radix);
                    rv += _digits[(int)r];
                    t2 = (t2 * radix) - r;

                    // forced break after 10 digits
                    maxdigit--;
                    if (maxdigit == 0)
                    {
                        break;
                    }
                }
                if (x < 0)
                {
                    rv = "-" + rv;
                }
            }
            return rv;
        }


        public static string Spaces(string val, int nr)
        {
            return Spaces(val, nr, ' ');
        }


        public static string Spaces(string val, int nr, char sep)
        {
            var rv = "";
            var j = 0;
            for (int i = val.Length - 1; i >= 0; i--)
            {
                j++;
                rv = val[i] + rv;
                if (j % nr == 0)
                {
                    rv = sep + rv;
                }
            }
            if (rv[0] == sep)
            {
                rv = rv.Substring(1);
            }
            return rv;
        }

        #endregion


        #region Methods

        /// <summary>
        ///     CheckArg checks the arguments for the encoder and decoder calls
        /// </summary>
        private static void CheckArg(long radix, bool sym)
        {
            if ((radix > 36) && (sym == false))
            {
                throw new Exception(_radixTooLargeErrorMessage1);
            }
            if (radix > 1000000)
            {
                throw new Exception(_radixTooLargeErrorMessage2);
            }
            if (radix < 2)
            {
                throw new Exception(_radixTooSmallErrorMessage);
            }
        }

        #endregion
    }
}