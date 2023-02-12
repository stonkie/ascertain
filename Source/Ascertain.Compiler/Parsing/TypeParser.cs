using System.Collections.ObjectModel;
using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

internal class TypeParser
{
    private readonly string _typeName;
    private readonly Modifier _typeModifiers;
    private readonly IReadOnlyList<CallSyntacticExpression> _compilerMetadata;

    private Modifier _activeModifiers = 0;
    private string? _activeName;
    private string? _activeType;
    
    private IReadOnlyList<SyntacticParameterDeclaration>? _activeParameterDeclarations;
    private ParameterDeclarationListParser? _activeParameterDeclarationParser;
    
    private IReadOnlyList<SyntacticParameterDeclaration>? _activeTypeParameterDeclarations;
    private ParameterDeclarationListParser? _activeTypeParameterDeclarationParser;

    private readonly List<SyntacticMember> _accumulatedMembers = new();
    
    private IMemberParser? _activeMemberParser;

    private bool _isCompleted;

    public TypeParser(string typeName, Modifier typeModifiers, IReadOnlyList<CallSyntacticExpression> compilerMetadata)
    {
        _typeName = typeName;
        _typeModifiers = typeModifiers;
        _compilerMetadata = compilerMetadata;
    }

    public SyntacticObjectType? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        if (_activeParameterDeclarationParser != null)
        {
            var parameterDeclarations = _activeParameterDeclarationParser.ParseToken(token);

            if (parameterDeclarations != null)
            {
                _activeParameterDeclarations = parameterDeclarations;
                _activeParameterDeclarationParser = null;
            }
            
            return null;
        }

        if (_activeTypeParameterDeclarationParser != null)
        {
            var parameterDeclarations = _activeTypeParameterDeclarationParser.ParseToken(token);

            if (parameterDeclarations != null)
            {
                _activeTypeParameterDeclarations = parameterDeclarations;
                _activeTypeParameterDeclarationParser = null;
            }
            
            return null;
        }

        if (_activeMemberParser != null)
        {
            var method = _activeMemberParser.ParseToken(token);

            if (method != null)
            {
                _accumulatedMembers.Add(method);
                _activeMemberParser = null;
            }
            
            return null;
        }
        
        var tokenValue = token.Value.Span;
        
        var modifier = tokenValue.ToModifier();

        if (modifier != null)
        {
            if (_activeParameterDeclarations != null || _activeTypeParameterDeclarations != null)
            {
                throw new AscertainException(AscertainErrorCode.ParserModifierAfterTypeOnMember,
                    $"Modifier {modifier} at {token.Position} should be placed before parameters");
            }
            
            _activeModifiers = _activeModifiers.AddForMethod(modifier.Value, token.Position);
            
            return null;
        }

        switch (tokenValue)
        {
            case "{":
            case "=":
                
                if (_activeName == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMissingNameInMemberDefinition,
                        $"Missing name in member definition at {token.Position}");
                }
                
                if (_activeType == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMemberWithoutReturnType,
                        $"Missing return type name in member definition at {token.Position}");
                }
                
                if (_activeParameterDeclarations == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMissingTypeInMemberDefinition,
                        $"Missing type declaration in member definition at {token.Position}");
                }

                if (tokenValue.Equals("=".AsSpan(), StringComparison.InvariantCulture))
                {
                    //TODO : property members not supported
                    throw new NotSupportedException("Properties not supported");
                    // if (_activeParameterDeclarations != null)
                    // {
                    //     throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonMethodMember,
                    //         $"Non method members cannot have a parameterized type at {token.Position}");
                    // }
                    //
                    // _activeMemberParser = new PropertyParser(_activeName, _activeModifiers, _activeTypeDeclaration);
                }

                var typeDeclaration = new TypeDeclaration(token.Position, _activeType, _activeParameterDeclarations, _activeTypeParameterDeclarations ?? new List<SyntacticParameterDeclaration>());
                    
                _activeMemberParser = new MethodParser(_activeName, _activeModifiers, typeDeclaration);

                _activeName = null;
                _activeType = null;
                _activeModifiers = 0;
                _activeParameterDeclarations = null;
                _activeTypeParameterDeclarations = null;

                return null;
            case "}":
                _isCompleted = true;
                return new SyntacticObjectType(token.Position, _typeName, _typeModifiers, _accumulatedMembers, _compilerMetadata);
            case "(":
                if (_activeType == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonTypeOnMember,
                        $"Token {tokenValue} at {token.Position} opens parameter definition, but is not preceded by a type name");
                }

                if (_activeParameterDeclarations != null || _activeParameterDeclarationParser != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedMoreThanOnceOnMember,
                        $"Token {tokenValue} at {token.Position} opens parameter definition, but a parameter definition was declared previously on this member");
                }

                _activeParameterDeclarationParser = new ParameterDeclarationListParser(false);
                return null;
            case "<":
                if (_activeType == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonTypeOnMember,
                        $"Token {tokenValue} at {token.Position} opens type parameter definition, but is not preceded by a type name");
                }

                if (_activeParameterDeclarations != null || _activeParameterDeclarationParser != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserTypeParameterListAfterParameterList,
                        $"Token {tokenValue} at {token.Position} opens type parameter definition, but a parameter definition was declared previously on this member");
                }

                if (_activeTypeParameterDeclarations != null || _activeTypeParameterDeclarationParser != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserDuplicateTypeParameterDeclaration,
                        $"Token {tokenValue} at {token.Position} opens type parameter definition, but a type parameter definition was declared previously on this member");
                }

                _activeTypeParameterDeclarationParser = new ParameterDeclarationListParser(true);
                return null;
            case ")":
            case ";":
            case ".":
            case "#":
            case "\"":
                throw new AscertainException(AscertainErrorCode.ParserIllegalCharacterInTypeDefinition,
                    $"Character {tokenValue} at {token.Position} is illegal in type definition");
        }

        if (_activeName == null)
        {
            _activeName = tokenValue.ToString();    
        }
        else if (_activeType == null)
        {
            _activeType = tokenValue.ToString();
        }
        else
        {
            throw new AscertainException(AscertainErrorCode.ParserTooManyIdentifiersOnMember,
                $"Token {tokenValue} at {token.Position} was found after member name {_activeName} and return type {_activeType}");
        }
        
        return null;
    }
}