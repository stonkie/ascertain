namespace Ascertain.Compiler.Analysis.Surface;

public record SurfaceObjectType(QualifiedName Name, Dictionary<string, List<SurfaceMember>> Members, CompilerPrimitiveType? Primitive) : ISurfaceType
{
    public bool AssignableTo(ISurfaceType destination)
    {
        // TODO : Implement assignation covariance
        if (destination is SurfaceObjectType objectDestination)
        {
            if (objectDestination.Primitive?.Type == PrimitiveType.Void)
            {
                return true;
            }
            
            return objectDestination.Name == Name;    
        }

        return false;
    }

    public override string ToString()
    {
        return Name.ToString();
    }
}