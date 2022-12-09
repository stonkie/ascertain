using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Surface;

public record AnonymousSurfaceCallableType(Position Position, ObjectTypeReference ReturnType, List<ParameterDeclaration> Parameters) : 
    SurfaceCallableType(ReturnType, Parameters), ITypeReference<SurfaceCallableType>
{
    public SurfaceCallableType ResolvedType => this;
}