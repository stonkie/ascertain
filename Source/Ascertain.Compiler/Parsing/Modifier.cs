namespace Ascertain.Compiler.Parsing;

[Flags]
public enum Modifier
{
    Class = 1,
    Public = 1 << 2,
    Static = 1 << 3,
}