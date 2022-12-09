namespace Ascertain.Compiler.Analysis.Surface;

/// <remarks>
/// Callable types support duck-typing.
/// </remarks>
public record SurfaceCallableType(ObjectTypeReference ReturnType, List<ParameterDeclaration> Parameters) : ISurfaceType
{
    public bool AssignableTo(ISurfaceType destination)
    {
        if (destination is SurfaceCallableType callableDestination)
        {
            if (Parameters.Count != callableDestination.Parameters.Count)
            {
                return false;
            }

            for (int index = 0; index < Parameters.Count; index++)
            {
                if (!Parameters[index].ObjectType.ResolvedType.AssignableTo(callableDestination.Parameters[index].ObjectType.ResolvedType))
                {
                    return false;
                }
            }

            return true;
        }
        
        return false;
    }
}

