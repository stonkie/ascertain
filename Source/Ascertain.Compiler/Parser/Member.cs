namespace Ascertain.Compiler.Parser;

public class Member : IMember
{
    public string Name { get; }
    public Modifier Modifiers { get; }
    public ITypeDeclaration TypeDeclaration { get; }
    public IStatement Statement { get; }

    public Member(string name, Modifier modifiers, ITypeDeclaration typeDeclaration, IStatement statement)
    {
        Name = name;
        Modifiers = modifiers;
        TypeDeclaration = typeDeclaration;
        Statement = statement;
    }
}