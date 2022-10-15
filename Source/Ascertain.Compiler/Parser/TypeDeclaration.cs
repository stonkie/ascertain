namespace Ascertain.Compiler.Parser;

public class TypeDeclaration : ITypeDeclaration
{
    public string ReturnTypeName { get; }
    public List<IParameterDeclaration>? ParameterDeclarations { get; }

    public TypeDeclaration(string returnTypeName, List<IParameterDeclaration>? parameterDeclarations)
    {
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
    }
}