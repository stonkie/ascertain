namespace Ascertain.Compiler.Analysis.Surface;

public record SurfaceObjectType(QualifiedName Name, Dictionary<string, List<SurfaceMember>> Members, CompilerPrimitive? Primitive) : ISurfaceType
{
    public bool AssignableTo(ISurfaceType destination)
    {
        // TODO : Implement assignation covariance
        if (destination is SurfaceObjectType objectDestination)
        {
            return objectDestination.Name == Name;    
        }

        return false;
    }
}