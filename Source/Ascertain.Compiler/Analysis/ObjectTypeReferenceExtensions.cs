using Ascertain.Compiler.Analysis.Surface;

namespace Ascertain.Compiler.Analysis;

public static class ObjectTypeReferenceExtensions
{
    public static SurfaceObjectType Get(this ObjectTypeReference reference)
    {
        if (reference.ResolvedType == null)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerUnresolvedReferenceAfterAnalysis, $"Unresolved reference to {reference.Name} was found after the analysis phase.");
        }

        return reference.ResolvedType;
    }
}