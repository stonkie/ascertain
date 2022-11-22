namespace Ascertain.Compiler.Parsing;

public class SyntacticObjectType
{
    public Position Position { get; }
    
    public List<Member> Members { get; }
    public string Name { get; }
    public Modifier Modifiers { get; }

    public SyntacticObjectType(Position position, string name, Modifier modifiers, List<Member> members)
    {
        Position = position;
        Members = members;
        Name = name;
        Modifiers = modifiers;
    }
}