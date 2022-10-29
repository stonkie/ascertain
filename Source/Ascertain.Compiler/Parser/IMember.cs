namespace Ascertain.Compiler.Parser;

public interface IMember
{
    string Name { get; }
    Modifier Modifiers { get; }
    TypeDeclaration TypeDeclaration { get; }
    IExpression Expression { get; }
}