using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Surface;

public record AnonymousSurfaceCallableType(Position Position, ITypeReference<SurfaceObjectType> ReturnType, List<SurfaceParameterDeclaration> Parameters, List<SurfaceParameterDeclaration> TypeParameters) : 
    SurfaceCallableType(ReturnType, Parameters, TypeParameters), ITypeReference<SurfaceCallableType>
{
    public SurfaceCallableType ResolvedType => this;
}