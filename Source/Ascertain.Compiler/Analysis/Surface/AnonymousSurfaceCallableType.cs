using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Surface;

public record AnonymousSurfaceCallableType(Position Position, ITypeReference<SurfaceObjectType> ReturnType, List<SurfaceParameterDeclaration> Parameters) : 
    SurfaceCallableType(ReturnType, Parameters), ITypeReference<SurfaceCallableType>
{
    public SurfaceCallableType ResolvedType => this;
}