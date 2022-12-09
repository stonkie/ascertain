using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Surface;

public record ObjectTypeReference(Position Position, QualifiedName Name) : ITypeReference<SurfaceObjectType>
{
    private SurfaceObjectType? _resolvedType;

    public SurfaceObjectType ResolvedType
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