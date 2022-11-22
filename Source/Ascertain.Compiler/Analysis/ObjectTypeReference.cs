namespace Ascertain.Compiler.Analysis;

public record ObjectTypeReference(Position Position, QualifiedName Name)
{
    public ObjectType? ResolvedType { get; set; }
    
    // TODO : Implement Function and Generic types 
}