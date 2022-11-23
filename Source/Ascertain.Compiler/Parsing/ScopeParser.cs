using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ScopeParser : IStatementParser
{
    private IStatementParser? _activeStatementParser = null;

    private readonly List<BaseExpression> _accumulatedStatements = new();
    
    private bool _isCompleted;

    private Position? _position = null;

    public BaseExpression? ParseToken(Token token)
    {
        _position ??= token.Position;
        
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }
        
        if (_activeStatementParser != null)
        {
            var statement = _activeStatementParser.ParseToken(token);

            if (statement != null)
            {
                _accumulatedStatements.Add(statement);
                _activeStatementParser = null;
            }
            
            return null;
        }
        
        var tokenValue = token.Value.Span;

        switch (tokenValue)
        {
            case "#":
            case "\"":
                throw new AscertainException(AscertainErrorCode.ParserIllegalTokenInScope,
                    $"Character {tokenValue} at {token.Position} is illegal in a scope definition");
            case "}":
                _isCompleted = true;
                return new Scope(_position.Value, _accumulatedStatements);
            case "{":
                _activeStatementParser = new ScopeParser();
                break;
            case ";":
                throw new AscertainException(AscertainErrorCode.ParserEmptyStatement, $"An empty state was found at {token.Position}");
            case "=":
                throw new AscertainException(AscertainErrorCode.ParserAssignmentOperatorWithoutTarget, $"An assignment operator without an assignment target was found at {token.Position}");
            case "(":
            case ")":
            case ".":
            // TODO : Add specialized statement parsers (e.g. for, if, while)
            default:
                _activeStatementParser = new ExpressionParser();
                
                var statement = _activeStatementParser.ParseToken(token);

                // In case of a single token statement like possibly a lone ";" 
                if (statement != null)
                {
                    _accumulatedStatements.Add(statement);
                    _activeStatementParser = null;
                    return null;
                }
                
                break;
        }
        return null;
    }
}