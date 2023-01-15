namespace Ascertain.Compiler.Analysis;

// TODO : Handle qualifying names through namespaces
public record QualifiedName(string Name)
{
    public override string ToString() => Name;
    
    public static QualifiedName Void => new ("Void");
    public static QualifiedName String => new ("String");
    public static QualifiedName Int32 => new ("Int32");
    public static QualifiedName Anonymous => new ("");
}