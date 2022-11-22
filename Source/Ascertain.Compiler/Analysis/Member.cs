namespace Ascertain.Compiler.Analysis;

public record Member(string Name, ObjectTypeReference ReturnType, List<ParameterDeclaration>? Parameters, bool IsPublic, bool IsStatic);

