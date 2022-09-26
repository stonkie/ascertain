namespace Ascertain.Compiler.Parser;

internal class TypeParser
{
    private readonly string _typeName;
    private readonly Modifier _typeModifiers;

    private Modifier _activeModifiers = 0;
    private string? _activeName;
    
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

        if (_activeMemberParser != null)
        {
            var method = _activeMemberParser.ParseToken(token);

            if (method != null)
            {
                _accumulatedMembers.Add(method);
            }
            
            return null;
        }
        
        var tokenValue = token.Value.Span;
        
        var modifier = tokenValue.ToModifier();

        if (modifier != null)
        {
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
            case ";":
            case "(":
            case ")":
            case ".":
                throw new AscertainException(AscertainErrorCode.ParserIllegalCharacterInTypeDefinition,
                    $"Character {tokenValue} at {token.Position} is illegal in type definition");
        }

        _activeName = tokenValue.ToString();
        return null;
    }
}