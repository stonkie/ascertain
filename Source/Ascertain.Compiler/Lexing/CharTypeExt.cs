namespace Ascertain.Compiler.Lexing;

public static class CharTypeExt
{
    public static bool IsOneTokenPerChar(this CharType type)
    {
        return type == CharType.Grouper;
    }
}