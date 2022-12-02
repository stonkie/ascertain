using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis;

public interface ITypeReference<out T> where T : BaseType
{
    T ResolvedType { get; }
    Position Position { get; }
    
    // TODO : Implement Function and Generic types 
}

public record ObjectTypeReference(Position Position, QualifiedName Name) : ITypeReference<ObjectType>
{
    private ObjectType? _resolvedType;

    public ObjectType ResolvedType
    {
        get
        {
            if (_resolvedType == null)
            {
                throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerUnresolvedReferenceAfterAnalysis,
                    $"Unresolved reference to {Name} was found after the analysis phase.");
            }

            return _resolvedType;
        }
        set => _resolvedType = value;
    }
}

public record AnonymousCallableType(Position Position, ObjectTypeReference ReturnType, List<ParameterDeclaration> Parameters) : CallableType(ReturnType, Parameters), ITypeReference<CallableType>
{
    public CallableType ResolvedType => this;
}