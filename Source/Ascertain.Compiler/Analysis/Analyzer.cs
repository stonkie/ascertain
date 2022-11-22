using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

internal class Analyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types; // TODO : Make types stream filterable by namespace
    private readonly string _soughtType; // Either a Program (new is main) or an Exposed (all public are C extern functions) object. 

    private readonly TypeRepository _typeRepository = new(); 
    
    public Analyzer(IAsyncEnumerable<SyntacticObjectType> types, string soughtType)
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
            else
            {
                ObjectType objectType = AnalyzeType(type);
                _typeRepository.Add(new QualifiedName(type.Name), objectType);

                if (type.Name == _soughtType)
                {
                    soughtObjectType = objectType;
                }
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
        
        return new ObjectType(new QualifiedName(type.Name), members);
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

        return new Member(
            member.Name,
            _typeRepository.GetTypeReference(member.TypeDeclaration.Position, new QualifiedName(member.TypeDeclaration.ReturnTypeName)), 
            parameterDeclarations.Select(AnalyzeParameterDeclaration).ToList(),
            isPublic,
            isStatic);
    }

    private ParameterDeclaration AnalyzeParameterDeclaration(SyntacticParameterDeclaration parameter)
    {
        var typeReference = _typeRepository.GetTypeReference(parameter.SyntacticTypeReference.Position, new QualifiedName(parameter.SyntacticTypeReference.Name));

        return new ParameterDeclaration(typeReference, parameter.Name);
    }
    
}