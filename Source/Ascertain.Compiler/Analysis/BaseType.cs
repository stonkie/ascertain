namespace Ascertain.Compiler.Analysis;

public abstract record BaseType()
{
    public abstract bool AssignableTo(BaseType destination);
}

public record ObjectType(QualifiedName Name, Dictionary<string, List<Member>> Members, CompilerPrimitive? Primitive) : BaseType()
{
    public override bool AssignableTo(BaseType destination)
    {
        // TODO : Implement assignation covariance
        if (destination is ObjectType objectDestination)
        {
            return objectDestination.Name == Name;    
        }

        return false;
    }
}

/// <remarks>
/// Callable types support duck-typing.
/// </remarks>
public record CallableType(ObjectTypeReference ReturnType, List<ParameterDeclaration> Parameters) : BaseType()
{
    public override bool AssignableTo(BaseType destination)
    {
        if (destination is CallableType callableDestination)
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

