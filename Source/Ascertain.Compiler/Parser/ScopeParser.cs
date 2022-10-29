﻿namespace Ascertain.Compiler.Parser;

public class ScopeParser : IStatementParser
{
    private IStatementParser? _activeStatementParser = null;

    private readonly List<IExpression> _accumulatedStatements = new();
    
    private bool _isCompleted;

    public IExpression? ParseToken(Token token)
    {
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
            case "}":
                _isCompleted = true;
                return new Scope(_accumulatedStatements);
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