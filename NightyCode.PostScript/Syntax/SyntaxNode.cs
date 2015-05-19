namespace NightyCode.PostScript.Syntax
{
    #region Namespace Imports

    using JetBrains.Annotations;

    #endregion


    public abstract class SyntaxNode
    {
        #region Properties

        [CanBeNull]
        public SyntaxBlock Parent
        {
            get;
            internal set;
        }

        public abstract string Text
        {
            get;
        }

        #endregion
    }
}