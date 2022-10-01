namespace Ascertain.Compiler.Parser;

public interface IObjectType
{
    string Name { get; }
    public Modifier Modifiers { get; }
    List<IMember> Members { get; }
}