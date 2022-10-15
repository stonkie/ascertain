namespace Ascertain.Compiler.Parser;

public interface ITypeDeclaration
{
    string ReturnTypeName { get; }
    List<IParameterDeclaration>? ParameterDeclarations { get; }
}