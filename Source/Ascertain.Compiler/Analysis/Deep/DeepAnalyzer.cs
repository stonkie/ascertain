using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Analysis.Deep;

public class DeepAnalyzer
{
    private readonly SurfaceAnalyzer _surfaceAnalyzer;
    private readonly string _soughtType; // Either a Program (new is main) or an Exposed (all public are C extern functions) object.

    private TypeRepository? _typeRepository;
    
    public DeepAnalyzer(SurfaceAnalyzer surfaceAnalyzer, string soughtType)
    {
        _surfaceAnalyzer = surfaceAnalyzer;
        _soughtType = soughtType;
    }

    public async Task<(SurfaceObjectType SoughtType, Func<SurfaceObjectType, ObjectType> TypeResolver)> GetSoughtType()
    {
        SurfaceObjectType? soughtType = null;
        
        // Materialize all surface types so references are resolved
        var allSurfaceTypes = await _surfaceAnalyzer.GetObjectTypes();
        
        _typeRepository = new(allSurfaceTypes);

        var accessibleSurfaceTypes = allSurfaceTypes;
        
        foreach (var surfaceType in allSurfaceTypes)
        {
            var constructorMember = new SurfaceCallableType(
                new ObjectTypeReference(new Position(0, 0), surfaceType.Name) {ResolvedType = surfaceType},
                new List<SurfaceParameterDeclaration>());
            
            Dictionary<string, List<Member>> members = new()
            {{"new", new List<Member> { 
                new ("new", 
                    constructorMember,
                    true,
                    true,
                    new NewExpression(surfaceType)
                )
            }}};

            foreach (var surfaceMember in surfaceType.Members.SelectMany(m => m.Value))
            {
                if (surfaceMember.ReturnType.ResolvedType is SurfaceCallableType surfaceCallableType)
                {
                    Dictionary<string, Variable> variables = new()
                    {
                        {"new", new Variable(constructorMember)}
                    };

                    foreach (var parameter in surfaceCallableType.Parameters)
                    {
                        // Differentiate mutables
                        variables.Add(parameter.Name, new Variable(parameter.ObjectType.ResolvedType));
                    }

                    var expressionAnalyzer = new ScopeAnalyzer(accessibleSurfaceTypes, surfaceMember.SyntacticExpression, variables, surfaceCallableType.ReturnType);

                    var scope = expressionAnalyzer.Analyze();

                    if (!scope.ObjectReturnType.AssignableTo(surfaceCallableType.ReturnType.ResolvedType))
                    {
                        var position = surfaceMember.SyntacticExpression.Statements.FirstOrDefault()?.Position ?? surfaceMember.SyntacticExpression.Position;
                        
                        throw new AscertainException(AscertainErrorCode.AnalyzerMethodImplementationReturnTypeIncompatibleWithDeclaration,
                            $"The method declaration at {position} has return type {surfaceCallableType.ReturnType.ResolvedType} which is incompatible with its implementation's {scope.ObjectReturnType} return type.");            
                    }
                    
                    // TODO : Implement overload resolution
                    // TODO : Prevent overloading "new"
                    members.Add(surfaceMember.Name, new List<Member>()
                    {
                        new(surfaceMember.Name, surfaceMember.ReturnType.ResolvedType, surfaceMember.IsPublic, surfaceMember.IsStatic, scope)
                    });
                }
                else
                {
                    throw new NotImplementedException("No implementation for properties yet");
                }
            }

            _typeRepository.Add(surfaceType, new ObjectType(surfaceType.Name, members.Select(m => (m.Key, m.Value.Single())).ToList(), surfaceType.Primitive));

            if (_soughtType == surfaceType.Name.Name)
            {
                soughtType = surfaceType;
            }
        }
        
        if (soughtType == null)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerSoughtMethodDoesNotExist,
                $"The sought type {_soughtType} was not found.");
        }

        return (soughtType, surfaceType => _typeRepository.Get(surfaceType));
    }
}