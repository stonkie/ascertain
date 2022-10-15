namespace Ascertain.Compiler.Parser;

public interface IMember
{
    string Name { get; }
    Modifier Modifiers { get; }
    ITypeDeclaration TypeDeclaration { get; }
    IStatement Statement { get; }
}