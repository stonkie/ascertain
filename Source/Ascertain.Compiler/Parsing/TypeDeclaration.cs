using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class TypeDeclaration
{
    public Position Position { get; }
    public string ReturnTypeName { get; }
    public IReadOnlyList<SyntacticParameterDeclaration>? ParameterDeclarations { get; } // null means not parameterized

    public TypeDeclaration(Position position, string returnTypeName, IReadOnlyList<SyntacticParameterDeclaration>? parameterDeclarations)
    {
        Position = position;
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
    }
}