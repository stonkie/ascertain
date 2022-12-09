using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis.Deep;

public record Scope(SurfaceObjectType ObjectReturnType, IReadOnlyList<BaseExpression> Expressions) : BaseExpression(ObjectReturnType);