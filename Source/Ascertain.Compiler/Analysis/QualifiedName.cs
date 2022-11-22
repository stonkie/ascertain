namespace Ascertain.Compiler.Analysis;

// TODO : Handle qualifying names through namespaces
public record QualifiedName(string Name)
{
    public override string ToString() => Name;
    
    public static QualifiedName Void => new QualifiedName("Void");
    public static QualifiedName Int32 => new QualifiedName("Int32");
}