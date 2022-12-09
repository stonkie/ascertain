using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis.Deep;

public record Member(string Name, ISurfaceType ReturnType, bool IsPublic, bool IsStatic, BaseExpression Expression);