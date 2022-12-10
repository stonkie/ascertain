namespace Ascertain.Compiler.Analysis.Deep;

public record ObjectType(QualifiedName Name, Dictionary<string, List<Member>> Members, CompilerPrimitiveType? Primitive);