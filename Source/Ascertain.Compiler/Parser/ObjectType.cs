namespace Ascertain.Compiler.Parser;

public class ObjectType : IObjectType
{
    public List<IMember> Members { get; }
    public string Name { get; }
    public Modifier Modifiers { get; }

    public ObjectType(string name, Modifier modifiers, List<IMember> members)
    {
        Members = members;
        Name = name;
        Modifiers = modifiers;
    }
}