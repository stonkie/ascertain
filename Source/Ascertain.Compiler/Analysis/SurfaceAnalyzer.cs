using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

internal class SurfaceAnalyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types; // TODO : Make types stream filterable by namespace
    private readonly string _soughtType; // Either a Program (new is main) or an Exposed (all public are C extern functions) object. 

    private readonly TypeRepository _typeRepository = new();
    private readonly TypeValidator _typeValidator = new();
    
    public SurfaceAnalyzer(IAsyncEnumerable<SyntacticObjectType> types, string soughtType)
    {
        _types = types;
        _soughtType = soughtType;
    }

    public async Task<ObjectType> GetObjectType()
    {
        ObjectType? soughtObjectType = null;
        
        // TODO : Prioritize search into referenced namespaces/files, trim unreferenced namespaces
        // TODO : Parallelize by file/parser and collect exceptions instead of aborting everything
        await foreach (SyntacticObjectType type in _types)
        {
            // TODO : Don't abort on the first exception
            if (_typeRepository.Contains(new QualifiedName(type.Name)))
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerMultipleTypesWithTheSameName, $"A type of name {type.Name} was found {type.Position}, but had already been found before.");
            }

            ObjectType objectType = AnalyzeType(type);
            _typeRepository.Add(new QualifiedName(type.Name), objectType);

            if (type.Name == _soughtType)
            {
                soughtObjectType = objectType;
            }
        }

        if (soughtObjectType == null)
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerSoughtTypeNotFound, $"The sought type {_soughtType} was not found.");
        }

        foreach (var unresolvedReference in _typeRepository.GetUnresolvedTypeReferences())
        {
            // TODO : Don't abort on the first error
            throw new AscertainException(AscertainErrorCode.AnalyzerUnresolvedReference, $"The type {unresolvedReference.Name} at {unresolvedReference.Position} could not be resolved to a declared type.");
        }
        
        _typeValidator.Validate();
        
        return soughtObjectType;
    }

    private ObjectType AnalyzeType(SyntacticObjectType type)
    {
        if (!type.Modifiers.HasFlag(Modifier.Class))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerNoCategoryModifierOnType, $"The type {type.Name} at {type.Position} does not have the class modifier.");
        }
        
        if (type.Modifiers.HasFlag(Modifier.Static))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerInvalidModifierOnType, $"The type {type.Name} at {type.Position} has the illegal static modifier.");
        }

        if (type.Modifiers.HasFlag(Modifier.Public))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerInvalidModifierOnType, $"The type {type.Name} at {type.Position} has the illegal public modifier.");
        }

        Dictionary<string, List<Member>> members = new();

        foreach (var syntacticMember in type.Members)
        {
            if (!members.ContainsKey(syntacticMember.Name))
            {
                members.Add(syntacticMember.Name, new List<Member>());
            }
            
            var member = AnalyzeMember(syntacticMember);
            members[syntacticMember.Name].Add(member);
        }

        foreach (CallSyntacticExpression metadata in type.CompilerMetadata)
        {
            if (metadata.Callable is AccessVariableSyntacticExpression variable)
            {
                switch (variable.Name)
                {
                    case "Primitive":
                        if (metadata.Parameters.Count != 1)
                        {
                            throw new AscertainException(AscertainErrorCode.AnalyzerPrimitiveCompilerMetadataInvalidParameters, $"The primitive compiler metadata at {metadata.Position} has an invalid number of parameters.");    
                        }

                        var primitiveType = metadata.Parameters.Single();
                        
                        break;
                    default:
                        throw new AscertainException(AscertainErrorCode.AnalyzerUnknownCompilerMetadata, $"The compiler metadata {variable.Name} at {metadata.Position} is unrecognized.");
                }
            }
            else
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerCompilerMetadataExpressionInvalid, $"The compiler metadata on {type.Name} at {metadata.Position} does not have a valid #Name(literal1, ...); format.");
            }
        }
        
        return new ObjectType(new QualifiedName(type.Name), members, null);
    }

    private Member AnalyzeMember(SyntacticMember member)
    {
        if (member.Modifiers.HasFlag(Modifier.Class))
        {
            throw new AscertainException(AscertainErrorCode.AnalyzerInvalidModifierOnMember, $"The member {member.Name} at {member.Position} has the illegal class modifier.");
        }

        bool isStatic = member.Modifiers.HasFlag(Modifier.Static);
        bool isPublic = member.Modifiers.HasFlag(Modifier.Public);

        var parameterDeclarations = member.TypeDeclaration.ParameterDeclarations;
        if (parameterDeclarations == null)
        {
            throw new NotImplementedException("There is no implementation for properties yet");
        }

        // TODO : Pass by reference return values and parameters can extend lifetime.
        var returnType = _typeRepository.GetTypeReference(member.TypeDeclaration.Position, new QualifiedName(member.TypeDeclaration.ReturnTypeName));
        var parameters = parameterDeclarations.Select(AnalyzeParameterDeclaration).ToList();
        
        Dictionary<string, Variable> variables = new();
 
        foreach (var parameter in parameters)
        {
            // Differentiate mutables
            variables.Add(parameter.Name, new Variable(parameter.ObjectType));
        }

        var expressionAnalyzer = new ScopeAnalyzer(_typeValidator, member.Expression, variables, returnType);

        var scope = expressionAnalyzer.Analyze();

        _typeValidator.AddImplicitCast(returnType, scope.ObjectReturnType, member.Expression.Statements.FirstOrDefault()?.Position ?? member.Expression.Position);

        return new Member(
            member.Name,
            new AnonymousCallableType(member.Position, returnType, parameters),
            isPublic,
            isStatic);
    }

    private ParameterDeclaration AnalyzeParameterDeclaration(SyntacticParameterDeclaration parameter)
    {
        var typeReference = _typeRepository.GetTypeReference(parameter.SyntacticTypeReference.Position, new QualifiedName(parameter.SyntacticTypeReference.Name));

        return new ParameterDeclaration(typeReference, parameter.Name);
    }
    
}
