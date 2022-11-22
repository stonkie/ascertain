namespace Ascertain.Compiler.Parsing;

public class Member
{
    public string Name { get; }
    public Modifier Modifiers { get; }
    public TypeDeclaration TypeDeclaration { get; }
    public IExpression Expression { get; }

    public Member(string name, Modifier modifiers, TypeDeclaration typeDeclaration, IExpression expression)
    {
        Name = name;
        Modifiers = modifiers;
        TypeDeclaration = typeDeclaration;
        Expression = expression;
    }
}