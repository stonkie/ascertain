namespace Ascertain.Compiler.Parsing;

public class TypeDeclaration
{
    public string ReturnTypeName { get; }
    public IReadOnlyCollection<ParameterDeclaration>? ParameterDeclarations { get; } // null means not parameterized

    public TypeDeclaration(string returnTypeName, IReadOnlyCollection<ParameterDeclaration>? parameterDeclarations)
    {
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
    }
}