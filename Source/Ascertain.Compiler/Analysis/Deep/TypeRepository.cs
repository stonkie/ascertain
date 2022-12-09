using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Deep;

public class TypeRepository
{
    private readonly IReadOnlyList<SurfaceObjectType> _allSurfaceTypes;
    private readonly List<(ITypeReference<SurfaceObjectType> Destination, ITypeReference<SurfaceObjectType>? Source, Position Position)> _implicitCasts = new();
    private readonly Dictionary<SurfaceObjectType, ObjectType> _completeTypes = new();
    
    public SurfaceObjectType VoidType { get; }

    public TypeRepository(IReadOnlyList<SurfaceObjectType> allSurfaceTypes)
    {
        _allSurfaceTypes = allSurfaceTypes;
        VoidType = allSurfaceTypes.Single(t => t.Primitive?.Type == PrimitiveType.Void);
    }

    public void Add(SurfaceObjectType surfaceType, ObjectType objectType)
    {
        if (_completeTypes.ContainsKey(surfaceType))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerSurfaceTypeDiscoveredMultipleTimes,
                $"The surface type {surfaceType} was found multiple time during deep analysis.");
        }
        
        _completeTypes.Add(surfaceType, objectType);
    }

    public ObjectType Get(SurfaceObjectType surfaceType)
    {
        if (!_completeTypes.ContainsKey(surfaceType))
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorAnalyzerSurfaceTypeWasNotDeepAnalyzed,
                $"The surface type {surfaceType} has skipped deep analysis.");
        }
        
        return _completeTypes[surfaceType];
    }

}