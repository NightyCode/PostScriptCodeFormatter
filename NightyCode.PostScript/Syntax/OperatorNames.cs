﻿namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using System.Collections.Generic;

    #endregion


    public static partial class SyntaxTreeBuilder
    {
        #region Constants and Fields

        private static readonly List<string> _operatorNames = new List<string>
        {
            "=",
            "==",
            "$error",
            "abs",
            "add",
            "aload",
            "anchorsearch",
            "and",
            "arc",
            "arcn",
            "arct",
            "arcto",
            "array",
            "ashow",
            "astore",
            "atan",
            "awidthshow",
            "begin",
            "bind",
            "bitshift",
            "bytesavailable",
            "cachestatus",
            "ceiling",
            "charpath",
            "clear",
            "cleardictstack",
            "cleartomark",
            "clip",
            "clippath",
            "cliprestore",
            "clipsave",
            "closefile",
            "closepath",
            "colorimage",
            "composefont",
            "concat",
            "concatmatrix",
            "copy",
            "copypage",
            "cos",
            "count",
            "countdictstack",
            "countexecstack",
            "counttomark",
            "cshow",
            "currentblackgeneration",
            "currentcacheparams",
            "currentcmykcolor",
            "currentcolor",
            "currentcolorrendering",
            "currentcolorscreen",
            "currentcolorspace",
            "currentcolortransfer",
            "currentdash",
            "currentdevparams",
            "currentdict",
            "currentfile",
            "currentflat",
            "currentfont",
            "currentglobal",
            "currentgray",
            "currentgstate",
            "currenthalftone",
            "currenthsbcolor",
            "currentlinecap",
            "currentlinejoin",
            "currentlinewidth",
            "currentmatrix",
            "currentmiterlimit",
            "currentobjectformat",
            "currentoverprint",
            "currentpacking",
            "currentpagedevice",
            "currentpoint",
            "currentrgbcolor",
            "currentscreen",
            "currentsmoothness",
            "currentstrokeadjust",
            "currentsystemparams",
            "currenttransfer",
            "currentundercolorremoval",
            "currentuserparams",
            "curveto",
            "cvi",
            "cvlit",
            "cvn",
            "cvr",
            "cvrs",
            "cvs",
            "cvx",
            "def",
            "defaultmatrix",
            "definefont",
            "defineresource",
            "defineuserobject",
            "deletefile",
            "dict",
            "dictstack",
            "div",
            "dtransform",
            "dup",
            "echo",
            "end",
            "eoclip",
            "eofill",
            "eq",
            "erasepage",
            "errordict",
            "exch",
            "exec",
            "execform",
            "execstack",
            "execuserobject",
            "executeonly",
            "executive",
            "exit",
            "exp",
            "file",
            "filenameforall",
            "fileposition",
            "fill",
            "filter",
            "findcolorrendering",
            "findencoding",
            "findfont",
            "findresource",
            "flattenpath",
            "floor",
            "flush",
            "flushfile",
            "for",
            "forall",
            "gcheck",
            "ge",
            "get",
            "getinterval",
            "globaldict",
            "glyphshow",
            "grestore",
            "grestoreall",
            "gsave",
            "gstate",
            "gt",
            "identmatrix",
            "idiv",
            "idtransform",
            "if",
            "ifelse",
            "image",
            "imagemask",
            "index",
            "ineofill",
            "infill",
            "initclip",
            "initgraphics",
            "initmatrix",
            "instroke",
            "inueofill",
            "inufill",
            "inustroke",
            "invertmatrix",
            "itransform",
            "known",
            "kshow",
            "languagelevel",
            "le",
            "length",
            "lineto",
            "ln",
            "load",
            "log",
            "loop",
            "lt",
            "makefont",
            "makepattern",
            "mark",
            "matrix",
            "maxlength",
            "mod",
            "moveto",
            "mul",
            "ne",
            "neg",
            "newpath",
            "noaccess",
            "not",
            "nulldevice",
            "or",
            "packedarray",
            "pathbbox",
            "pathforall",
            "pop",
            "print",
            "printobject",
            "product",
            "prompt",
            "pstack",
            "put",
            "putinterval",
            "quit",
            "rand",
            "rcheck",
            "rcurveto",
            "read",
            "readhexstring",
            "readline",
            "readonly",
            "readstring",
            "realtime",
            "rectclip",
            "rectfill",
            "rectstroke",
            "renamefile",
            "repeat",
            "resetfile",
            "resourceforall",
            "resourcestatus",
            "restore",
            "reversepath",
            "revision",
            "rlineto",
            "rmoveto",
            "roll",
            "rootfont",
            "rotate",
            "round",
            "rrand",
            "run",
            "save",
            "scale",
            "scalefont",
            "search",
            "selectfont",
            "serialnumber",
            "setbbox",
            "setblackgeneration",
            "setcachedevice",
            "setcachedevice2",
            "setcachelimit",
            "setcacheparams",
            "setcharwidth",
            "setcmykcolor",
            "setcolor",
            "setcolorrendering",
            "setcolorscreen",
            "setcolorspace",
            "setcolortransfer",
            "setdash",
            "setdevparams",
            "setfileposition",
            "setflat",
            "setfont",
            "setglobal",
            "setgray",
            "setgstate",
            "sethalftone",
            "sethsbcolor",
            "setlinecap",
            "setlinejoin",
            "setlinewidth",
            "setmatrix",
            "setmiterlimit",
            "setobjectformat",
            "setoverprint",
            "setpacking",
            "setpagedevice",
            "setpattern",
            "setrgbcolor",
            "setscreen",
            "setsmoothness",
            "setstrokeadjust",
            "setsystemparams",
            "settransfer",
            "setucacheparams",
            "setundercolorremoval",
            "setuserparams",
            "setvmthreshold",
            "shfill",
            "show",
            "showpage",
            "sin",
            "sqrt",
            "srand",
            "stack",
            "start",
            "startjob",
            "status",
            "statusdict",
            "stop",
            "stopped",
            "store",
            "string",
            "stringwidth",
            "stroke",
            "strokepath",
            "sub",
            "systemdict",
            "token",
            "transform",
            "translate",
            "truncate",
            "type",
            "uappend",
            "ucache",
            "ucachestatus",
            "ueofill",
            "ufill",
            "undef",
            "undefinefont",
            "undefineresource",
            "undefineuserobject",
            "upath",
            "userdict",
            "usertime",
            "ustroke",
            "ustrokepath",
            "version",
            "vmreclaim",
            "vmstatus",
            "wcheck",
            "where",
            "widthshow",
            "write",
            "writehexstring",
            "writeobject",
            "writestring",
            "xcheck",
            "xor",
            "xshow",
            "xyshow",
            "yshow"
        };

        #endregion
    }
}