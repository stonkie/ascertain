using Ascertain.Compiler.Analysis.Deep;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis.Surface;

public class SurfaceAnalyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types; // TODO : Make types stream prioritizable by sought type/namespace

    private readonly SurfaceTypeRepository _surfaceTypeRepository = new();

    public SurfaceAnalyzer(IAsyncEnumerable<SyntacticObjectType> types)
    {
        _types = types;
    }

    public async Task<IReadOnlyList<SurfaceObjectType>> GetObjectTypes()
    {
        // TODO : Prioritize search into referenced namespaces/files, trim unreferenced namespaces
        // TODO : Parallelize by file/parser and collect exceptions instead of aborting everything
        await foreach (SyntacticObjectType type in _types)
        {
            // TODO : Don't abort on the first exception
            if (_surfaceTypeRepository.Contains(new QualifiedName(type.Name)))
            {
                throw new AscertainException(AscertainErrorCode.AnalyzerMultipleTypesWithTheSameName, $"A type of name {type.Name} was found {type.Position}, but had already been found before.");
            }

            SurfaceObjectType surfaceObjectType = AnalyzeType(type);
            _surfaceTypeRepository.Add(new QualifiedName(type.Name), surfaceObjectType);
        }
        
        foreach (var unresolvedReference in _surfaceTypeRepository.GetUnresolvedTypeReferences())
        {
            // TODO : Don't abort on the first error
            throw new AscertainException(AscertainErrorCode.AnalyzerUnresolvedReference, $"The type {unresolvedReference.Name} at {unresolvedReference.Position} could not be resolved to a declared type.");
        }

        return _surfaceTypeRepository.GetAllTypes();
    }

    private SurfaceObjectType AnalyzeType(SyntacticObjectType type)
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

        Dictionary<string, List<SurfaceMember>> members = new();

        foreach (var syntacticMember in type.Members)
        {
            if (!members.ContainsKey(syntacticMember.Name))
            {
                members.Add(syntacticMember.Name, new List<SurfaceMember>());
            }
            
            var member = AnalyzeMember(syntacticMember);
            members[syntacticMember.Name].Add(member);
        }

        CompilerPrimitive? primitive = null;

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

                        BaseSyntacticExpression primitiveType = metadata.Parameters.Single();

                        if (primitiveType is AccessVariableSyntacticExpression primitiveValueExpression)
                        {
                            // TODO : Put string literal parsing in a common place 
                            if (primitiveValueExpression.Name.Length >= 2 && primitiveValueExpression.Name.StartsWith('"') && primitiveValueExpression.Name.EndsWith('"'))
                            {
                                var primitiveValue = primitiveValueExpression.Name.AsSpan(1, primitiveValueExpression.Name.Length - 2);

                                switch (primitiveValue)
                                {
                                    case "void":
                                        primitive = new CompilerPrimitive(PrimitiveType.Void);    
                                        break;
                                    default:
                                        throw new AscertainException(AscertainErrorCode.AnalyzerPrimitiveCompilerMetadataUnknownPrimitiveType, $"The primitive compiler metadata at {metadata.Position} declared unknown primitive type {primitiveValue}."); 
                                }
                            }
                            else
                            {
                                throw new AscertainException(AscertainErrorCode.AnalyzerPrimitiveCompilerMetadataParameterIsNotStringLiteral, $"The primitive compiler metadata at {metadata.Position} must have a single string literal parameter.");
                            }
                        }
                        else
                        {
                            throw new AscertainException(AscertainErrorCode.AnalyzerPrimitiveCompilerMetadataParameterIsNotStringLiteral, $"The primitive compiler metadata at {metadata.Position} must have a single token parameter."); 
                        }

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
        
        return new SurfaceObjectType(new QualifiedName(type.Name), members, primitive);
    }

    private SurfaceMember AnalyzeMember(SyntacticMember member)
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
        var returnType = _surfaceTypeRepository.GetTypeReference(member.TypeDeclaration.Position, new QualifiedName(member.TypeDeclaration.ReturnTypeName));
        var parameters = parameterDeclarations.Select(AnalyzeParameterDeclaration).ToList();
        
        return new SurfaceMember(
            member.Name,
            new AnonymousSurfaceCallableType(member.Position, returnType, parameters),
            isPublic,
            isStatic,
            member.Expression);
    }

    private ParameterDeclaration AnalyzeParameterDeclaration(SyntacticParameterDeclaration parameter)
    {
        var typeReference = _surfaceTypeRepository.GetTypeReference(parameter.SyntacticTypeReference.Position, new QualifiedName(parameter.SyntacticTypeReference.Name));

        return new ParameterDeclaration(typeReference, parameter.Name);
    }
    
}
