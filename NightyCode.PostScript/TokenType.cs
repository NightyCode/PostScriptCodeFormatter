namespace NightyCode.PostScript
{
    public enum TokenType
    {
        Comment,
        String,
        DictionaryStart,
        DictionaryEnd,
        ArrayStart,
        ArrayEnd,
        ProcedureStart,
        ProcedureEnd,
        LiteralName,
        ExecutableName,
        IntegerNumber,
        RealNumber
    }
}