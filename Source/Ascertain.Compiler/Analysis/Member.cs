namespace Ascertain.Compiler.Analysis;

public record Member(string Name, ITypeReference<BaseType> ReturnType, bool IsPublic, bool IsStatic);

