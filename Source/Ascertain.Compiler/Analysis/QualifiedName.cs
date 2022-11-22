namespace Ascertain.Compiler.Analysis;

// TODO : Handle qualifying names through namespaces
public record QualifiedName(string Name)
{
    public override string ToString() => Name;
}