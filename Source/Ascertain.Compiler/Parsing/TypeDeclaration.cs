using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class TypeDeclaration
{
    public Position Position { get; }
    public string ReturnTypeName { get; }
    public IReadOnlyList<SyntacticParameterDeclaration>? ParameterDeclarations { get; } // null means not parameterized
    public IReadOnlyList<SyntacticParameterDeclaration> TypeParameterDeclarations { get; } // null means no type parameters

    public TypeDeclaration(Position position, string returnTypeName, IReadOnlyList<SyntacticParameterDeclaration>? parameterDeclarations,
        IReadOnlyList<SyntacticParameterDeclaration> typeParameterDeclarations)
    {
        Position = position;
        ReturnTypeName = returnTypeName;
        ParameterDeclarations = parameterDeclarations;
        TypeParameterDeclarations = typeParameterDeclarations;
    }
}