using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ExpressionParser : IStatementParser
{
    private Token? _activeOperator = null;
    private IExpression? _activeExpression = null;

    private ExpressionParser? _activeAssignationSource = null;
    private ParameterListParser? _activeParameterListParser = null; 
    
    private bool _isCompleted;

    public IExpression? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        if (_activeAssignationSource != null)
        {
            IExpression? assignationSource = _activeAssignationSource.ParseToken(token);

            if (assignationSource != null)
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserAssignationIntoNullDestination, $"The parser accepted an assignation into an empty destination expression at {token.Position}");
                }
                
                _isCompleted = true;
                return new AssignationExpression(_activeExpression, assignationSource);
            }
            
            return null;
        }
        
        if (_activeParameterListParser != null)
        {
            List<IExpression>? parameters = _activeParameterListParser.ParseToken(token);
            if (parameters != null)
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserCallOnNullExpression, $"The parser accepted a call for an empty destination expression at {token.Position}");
                }

                _activeExpression = new CallExpression(_activeExpression, parameters);
                _activeParameterListParser = null;
            }

            return null;
        }
        
        var tokenValue = token.Value.Span;

        switch (tokenValue)
        {
            case ";":
                _isCompleted = true;

                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserNullStatement, $"The identifier at {token.Position} closes an empty expression.");
                }
                return _activeExpression;
            case "(":
                // Support for prioritization parenthesis here (create a child expression)
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserOpeningParenthesisOnNullStatement, $"The identifier at {token.Position} opens a parameter list for a call without an previous expression to call.");
                }
                
                _activeParameterListParser = new ParameterListParser();
                return null;
            case "=":
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserAssignationIntoNullStatement, $"The identifier at {token.Position} produces an assignation towards an empty destination expression.");
                }
                
                _activeAssignationSource = new ExpressionParser();
                return null;
            case ".":
                if (_activeOperator != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserMultipleContiguousOperators, $"The identifier at {token.Position} is an operator and it follows another operator.");    
                }

                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserAccessMemberOperatorOnNullStatement, $"The identifier at {token.Position} is an access member operator, but it is called without a previous expression.");
                }
                
                _activeOperator = token;
                return null;
            case "{":
            case "}":
            case ")":
            case "#":
                throw new AscertainException(AscertainErrorCode.ParserIllegalOperatorInStatement, $"The identifier at {token.Position} follows an expression without an operator.");
        }

        if (_activeOperator != null)
        {
            switch (_activeOperator.Value.Value.Span)
            {
                case ".":
                    if (_activeExpression == null)
                    {
                        throw new AscertainException(AscertainErrorCode.InternalErrorParserAccessMemberOnNullExpression, $"The identifier at {token.Position} is attempting to define an access member on a null expression.");    
                    }

                    _activeExpression = new AccessMemberExpression(_activeExpression, token);
                    break;
                default:
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserUnknownOperator, $"The identifier at {token.Position} was detected as an operator but it is has no implementation.");
            }
        }
        else if (_activeOperator == null)
        {
            if (_activeExpression != null)
            {
                throw new AscertainException(AscertainErrorCode.ParserIdentifiersNotSeparatedByOperator, $"The identifier at {token.Position} follows an expression without an operator.");
            }
            
            _activeExpression = new VariableExpression(token);
            return null;
        }
        
        return null;
    }
}

public class VariableExpression : IExpression
{
    public Token Token { get; }

    public VariableExpression(Token token)
    {
        Token = token;
    }
}

public class CallExpression : IExpression
{
    public IExpression Callable { get; }
    public List<IExpression> Parameters { get; }

    public CallExpression(IExpression callable, List<IExpression> parameters)
    {
        Callable = callable;
        Parameters = parameters;
    }
}

public class AssignationExpression : IExpression
{
    private readonly IExpression _destination;
    private readonly IExpression _source;

    public AssignationExpression(IExpression destination, IExpression source)
    {
        _destination = destination;
        _source = source;
    }
}

public class AccessMemberExpression : IExpression
{
    public IExpression Parent { get; }
    public Token Member { get; }

    public AccessMemberExpression(IExpression parent, Token member)
    {
        Parent = parent;
        Member = member;
    }
}

public class ParameterListParser
{
    public List<IExpression>? ParseToken(Token token)
    {
        var tokenValue = token.Value.Span;

        if (tokenValue.Equals(")".AsSpan(), StringComparison.InvariantCulture))
        {
            // TODO : Complete parameter list parsing
            return new List<IExpression>();
        }

        return null;
    }
}