namespace Ascertain.Compiler.Parser;

public class MethodParser : IMemberParser
{
    private readonly string _methodName;
    private readonly Modifier _methodModifiers;
    private readonly ITypeDeclaration _typeDeclaration;

    private readonly IStatementParser _activeScopeParser;
    
    private bool _isCompleted;
    
    public MethodParser(string methodName, Modifier methodModifiers, ITypeDeclaration typeDeclaration)
    {
        _methodName = methodName;
        _methodModifiers = methodModifiers;
        _typeDeclaration = typeDeclaration;
        _activeScopeParser = new ScopeParser();
    }

    public IMember? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        var scope = _activeScopeParser.ParseToken(token);

        if (scope != null)
        {
            _isCompleted = true;
            return new Member(_methodName, _methodModifiers, _typeDeclaration, scope);
        }
        
        return null;
    }
}

public class ScopeParser : IStatementParser
{
    private IStatementParser? _activeStatementParser = null;

    private List<IStatement> _accumulatedStatements = new();
    
    private bool _isCompleted;

    public IStatement? ParseToken(Token token)
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
            // TODO : Add specialized statement parsers (e.g. for, if, while)
            default:
                _activeStatementParser = new StatementParser();
                
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

public interface IScope : IStatement
{
    IReadOnlyCollection<IStatement> Statements { get; }
}

public class Scope : IScope
{
    public IReadOnlyCollection<IStatement> Statements { get; }

    public Scope(IReadOnlyCollection<IStatement> statements)
    {
        Statements = statements;
    }
}

public interface IStatement
{
}

public class Statement : IStatement
{
}

public interface IStatementParser
{
    IStatement? ParseToken(Token token);
}

public class StatementParser : IStatementParser
{
    public IStatement? ParseToken(Token token)
    {
        var tokenValue = token.Value.Span;

        if (tokenValue.Equals(";".AsSpan(), StringComparison.InvariantCulture))
        {
            return new Statement();
        }

        return null;
    }
}