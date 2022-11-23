namespace Ascertain.Compiler.Analysis;

public enum PrimitiveType
{
    Void,
    
}

public record CompilerPrimitive(PrimitiveType Type);