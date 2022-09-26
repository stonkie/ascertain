namespace Ascertain.Compiler.Parser;

[Flags]
public enum Modifier
{
    Class = 1,
    Private = 1 << 1,
    Public = 1 << 2,
    
}