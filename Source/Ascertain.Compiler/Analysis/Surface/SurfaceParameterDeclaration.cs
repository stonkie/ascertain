namespace Ascertain.Compiler.Analysis.Surface;

public record SurfaceParameterDeclaration(ITypeReference<SurfaceObjectType> ObjectType, string Name);