using Ascertain.Compiler.Analysis;

namespace Ascertain.Compiler.Generation;

public static class ObjectTypeReferenceExtensions
{
    public static ObjectType Get(this ObjectTypeReference reference)
    {
        if (reference.ResolvedType == null)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorGeneratorUnresolvedReference, $"Unresolved reference to {reference.Name} reached the Generator.");
        }

        return reference.ResolvedType;
    }
}