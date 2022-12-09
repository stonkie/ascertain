using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Surface;

public interface ITypeReference<out T> where T : ISurfaceType
{
    T ResolvedType { get; }
    Position Position { get; }
    
    // TODO : Implement Function and Generic types 
}