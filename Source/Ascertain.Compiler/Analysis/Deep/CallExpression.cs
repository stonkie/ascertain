using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis.Deep;

public record CallExpression(SurfaceObjectType ObjectReturnType, BaseExpression Method, List<BaseExpression> Parameters) : BaseExpression(ObjectReturnType);