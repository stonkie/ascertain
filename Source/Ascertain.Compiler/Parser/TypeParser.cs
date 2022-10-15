namespace Ascertain.Compiler.Parser;

internal class TypeParser
{
    private readonly string _typeName;
    private readonly Modifier _typeModifiers;

    private Modifier _activeModifiers = 0;
    private ITypeDeclaration? _activeTypeDeclaration;
    private string? _activeName;
    private IParameterDeclarationListParser? _activeTypeDeclarationParser;

    private readonly List<IMember> _accumulatedMembers = new();
    
    private IMemberParser? _activeMemberParser;

    private bool _isCompleted;

    public TypeParser(string typeName, Modifier typeModifiers)
    {
        _typeName = typeName;
        _typeModifiers = typeModifiers;
    }

    public IObjectType? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        if (_activeTypeDeclarationParser != null)
        {
            var typeDeclaration = _activeTypeDeclarationParser.ParseToken(token);

            if (typeDeclaration != null)
            {
                _activeTypeDeclaration = typeDeclaration;
                _activeTypeDeclarationParser = null;
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
            if (_activeTypeDeclaration != null)
            {
                throw new AscertainException(AscertainErrorCode.ParserModifierAfterTypeOnMember,
                    $"Modifier {modifier} at {token.Position} should be placed before type {_activeTypeDeclaration}");
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
                
                if (_activeTypeDeclaration == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMissingTypeInMemberDefinition,
                        $"Missing type declaration in member definition at {token.Position}");
                }

                if (tokenValue.Equals("=".AsSpan(), StringComparison.InvariantCulture))
                {
                    if (_activeTypeDeclaration.ParameterDeclarations != null)
                    {
                        throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonMethodMember,
                            $"Non method members cannot have a parameterized type at {token.Position}");
                    }
                    
                    _activeMemberParser = new PropertyParser(_activeName, _activeModifiers, _activeTypeDeclaration);
                }
                else
                {
                    _activeMemberParser = new MethodParser(_activeName, _activeModifiers, _activeTypeDeclaration);
                }
                
                _activeName = null;
                _activeModifiers = 0;
                _activeTypeDeclaration = null;
                
                return null;
            case "}":
                _isCompleted = true;
                return new ObjectType(_typeName, _typeModifiers, _accumulatedMembers);
            case "(":
                if (_activeTypeDeclaration == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonTypeOnMember,
                        $"Token {tokenValue} at {token.Position} opens parameter definition, but is not preceded by a type name");
                }

                if (_activeTypeDeclaration.ParameterDeclarations != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedMoreThanOnceOnMember,
                        $"Token {tokenValue} at {token.Position} opens parameter definition, but a parameter definition was declared previously on this member");
                }

                _activeTypeDeclarationParser = new ParameterDeclarationListParser(_activeTypeDeclaration.ReturnTypeName);
                _activeTypeDeclaration = null;
                return null;
            case ")":
            case ";":
            case ".":
                throw new AscertainException(AscertainErrorCode.ParserIllegalCharacterInTypeDefinition,
                    $"Character {tokenValue} at {token.Position} is illegal in type definition");
        }

        if (_activeName == null)
        {
            _activeName = tokenValue.ToString();    
        }
        else if (_activeTypeDeclaration == null)
        {
            _activeTypeDeclaration = new TypeDeclaration(tokenValue.ToString(), null);
        }
        else
        {
            throw new AscertainException(AscertainErrorCode.ParserTooManyIdentifiersOnMember,
                $"Token {tokenValue} at {token.Position} was found after member name {_activeName} and type {_activeTypeDeclaration}");
        }
        
        return null;
    }
}