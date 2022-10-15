namespace Ascertain.Compiler.Parser;

internal class TypeParser
{
    private readonly string _typeName;
    private readonly Modifier _typeModifiers;

    private Modifier _activeModifiers = 0;
    private string? _activeTypeName;
    private string? _activeName;
    private IParameterDeclarationListParser? _activeParameterListParser;
    private List<IParameterDeclaration>? _activeParameterDeclarationList = null; 
    
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

        if (_activeParameterListParser != null)
        {
            var parameterList = _activeParameterListParser.ParseToken(token);

            if (parameterList != null)
            {
                _activeParameterDeclarationList = parameterList;
                _activeParameterListParser = null;
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
            if (_activeTypeName != null)
            {
                throw new AscertainException(AscertainErrorCode.ParserModifierAfterTypeOnMember,
                    $"Modifier {modifier} at {token.Position} should be placed before type {_activeTypeName}");
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
                    throw new AscertainException(AscertainErrorCode.ParserMissingNameInTypeDefinition,
                        $"Missing name in member definition at {token.Position}");
                }

                if (tokenValue == "=".AsSpan())
                {
                    _activeMemberParser = new PropertyParser(_activeName, _activeModifiers);
                }
                else
                {
                    _activeMemberParser = new MethodParser(_activeName, _activeModifiers);    
                }
                
                _activeName = null;
                _activeModifiers = 0;
                
                return null;
            case "}":
                _isCompleted = true;
                return new ObjectType(_typeName, _typeModifiers, _accumulatedMembers);
            case "(":
                if (_activeTypeName != null)
                {
                    _activeParameterListParser = new ParameterDeclarationListParser();
                    return null;
                }
                else
                {
                    throw new AscertainException(AscertainErrorCode.ParserParametersAppliedOnNonTypeOnMember,
                        $"Token {tokenValue} at {token.Position} opens parameter definition, but is not preceded by a type name");        
                }
                break;
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
        else if (_activeTypeName == null)
        {
            _activeTypeName = tokenValue.ToString();
        }
        else
        {
            throw new AscertainException(AscertainErrorCode.ParserTooManyIdentifiersOnMember,
                $"Token {tokenValue} at {token.Position} was found after member name {_activeName} and type {_activeTypeName}");
        }
        
        return null;
    }
}