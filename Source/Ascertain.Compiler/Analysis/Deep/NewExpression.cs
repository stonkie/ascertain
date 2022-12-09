using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis.Deep;

public record NewExpression(SurfaceObjectType SurfaceType) : BaseExpression(SurfaceType);