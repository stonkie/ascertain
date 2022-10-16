namespace Ascertain.Compiler.Parser;

public class TypeDeclaration : ITypeDeclaration
{
    public string ReturnTypeName { get; }
    public IReadOnlyCollection<IParameterDeclaration>? ParameterDeclarations { get; } // null means not parameterized

    public TypeDeclaration(string returnTypeName, IReadOnlyCollection<IParameterDeclaration>? parameterDeclarations)
    {
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
    }
}