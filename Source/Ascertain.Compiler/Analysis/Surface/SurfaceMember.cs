using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis.Surface;

public record SurfaceMember(string Name, ITypeReference<ISurfaceType> ReturnType, bool IsPublic, bool IsStatic, ScopeSyntacticExpression SyntacticExpression);

