namespace Ascertain.Compiler.Parsing;

public class FileParser
{
    private Modifier _activeModifiers = 0;
    private string? _activeName;
    private TypeParser? _activeTypeParser;
    
    public SyntacticObjectType? ParseToken(Token token)
    {
        if (_activeTypeParser != null)
        {
            var rootType = _activeTypeParser.ParseToken(token);

            if (rootType != null)
            {
                _activeTypeParser = null;
            }
            
            return rootType;
        }
        
        var tokenValue = token.Value.Span;

        var modifier = tokenValue.ToModifier();

        if (modifier != null)
        {
            _activeModifiers = _activeModifiers.AddForType(modifier.Value, token.Position);
            return null;
        }

        switch (tokenValue)
        {
            case "{":
                if (_activeName == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMissingNameInTypeDefinition,
                        $"Missing name in type definition at {token.Position}");    
                }
                
                _activeTypeParser = new TypeParser(_activeName, _activeModifiers);
                _activeName = null;
                _activeModifiers = 0;
                return null;
            case "}":
                throw new AscertainException(AscertainErrorCode.ParserMismatchedClosingScopeAtRootLevel,
                    $"Mismatched closing scope character '}}' on file root at {token.Position}");
            case "(":
            case ")":
            case ".":
            case ";":
            case "=":
                throw new AscertainException(AscertainErrorCode.ParserIllegalCharacterInTypeDefinition,
                    $"Character {tokenValue} at {token.Position} is illegal in type definition");
        }

        _activeName = tokenValue.ToString();
        return null;
    }
}