namespace Ascertain.Compiler.Analysis.Surface;

/// <remarks>
/// Callable types support duck-typing.
/// </remarks>
public record SurfaceCallableType(ITypeReference<SurfaceObjectType> ReturnType, List<SurfaceParameterDeclaration> Parameters, List<SurfaceParameterDeclaration> TypeParameters) : ISurfaceType
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

            if (TypeParameters.Count != callableDestination.TypeParameters.Count)
            {
                return false;
            }

            for (int index = 0; index < TypeParameters.Count; index++)
            {
                if (!TypeParameters[index].ObjectType.ResolvedType.AssignableTo(callableDestination.TypeParameters[index].ObjectType.ResolvedType))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        return false;
    }
}

