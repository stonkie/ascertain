using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class FileParser
{
    private Modifier _activeModifiers = 0;
    private string? _activeName;
    private List<CallExpression> _activeCompilerMetadata = new();
    private TypeParser? _activeTypeParser;
    private ExpressionParser? _activeCompilerMetadataParser;
    
    public SyntacticObjectType? ParseToken(Token token)
    {
        if (_activeCompilerMetadataParser != null)
        {
            IExpression? compilerMetadataExpression = _activeCompilerMetadataParser.ParseToken(token);

            if (compilerMetadataExpression != null)
            {
                if (compilerMetadataExpression is CallExpression callExpressionn)
                {
                    _activeCompilerMetadata.Add(callExpressionn);    
                }
                else
                {
                    // TODO : Don't abort when this exception occurs
                    throw new AscertainException(AscertainErrorCode.ParserCompilerMetadataIsNotCallExpression,
                        $"Compiler metadata at {token.Position} cannot be parsed as a call expression. Type is {compilerMetadataExpression.GetType().FullName}");
                }
                _activeCompilerMetadataParser = null;
            }
            
            return null;
        }
        
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
            case "#":
                _activeCompilerMetadataParser = new ExpressionParser();
                break;
            case "{":
                if (_activeName == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMissingNameInTypeDefinition,
                        $"Missing name in type definition at {token.Position}");
                }
                
                _activeTypeParser = new TypeParser(_activeName, _activeModifiers, _activeCompilerMetadata);
                _activeName = null;
                _activeModifiers = 0;
                _activeCompilerMetadata = new();
                return null;
            case "}":
                throw new AscertainException(AscertainErrorCode.ParserMismatchedClosingScopeAtRootLevel,
                    $"Mismatched closing scope character '}}' on file root at {token.Position}");
            case "(":
            case ")":
            case ".":
            case ";":
            case "=":
            case "\"":
                throw new AscertainException(AscertainErrorCode.ParserIllegalCharacterInTypeDefinition,
                    $"Character {tokenValue} at {token.Position} is illegal in type definition");
        }

        _activeName = tokenValue.ToString();
        return null;
    }
}