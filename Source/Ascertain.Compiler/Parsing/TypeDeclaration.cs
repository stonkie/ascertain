namespace Ascertain.Compiler.Parsing;

public class TypeDeclaration
{
    public string ReturnTypeName { get; }
    public IReadOnlyCollection<IParameterDeclaration>? ParameterDeclarations { get; } // null means not parameterized

    public TypeDeclaration(string returnTypeName, IReadOnlyCollection<IParameterDeclaration>? parameterDeclarations)
    {
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
    }
}