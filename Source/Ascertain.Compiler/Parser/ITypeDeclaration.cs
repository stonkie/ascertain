namespace Ascertain.Compiler.Parser;

public interface ITypeDeclaration
{
    string ReturnTypeName { get; }
    IReadOnlyCollection<IParameterDeclaration>? ParameterDeclarations { get; }
}