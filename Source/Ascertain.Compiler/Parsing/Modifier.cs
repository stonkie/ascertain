namespace Ascertain.Compiler.Parsing;

[Flags]
public enum Modifier
{
    Class = 1,
    Public = 1 << 1,
    Static = 1 << 2,
}