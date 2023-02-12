using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ExpressionParser : IStatementParser
{
    private readonly bool _isCompilerDirective;
    private Token? _activeOperator = null;
    private BaseSyntacticExpression? _activeExpression = null;

    private ExpressionParser? _activeAssignationSource = null;
    private ParameterListParser? _activeParameterListParser = null;
    private ParameterListParser? _activeTypeParameterListParser = null;

    private List<BaseSyntacticExpression>? _parsedTypeParameters = null;
    
    private bool _isCompleted;

    public ExpressionParser(bool isCompilerDirective)
    {
        _isCompilerDirective = isCompilerDirective;
    }

    public BaseSyntacticExpression? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        if (_activeAssignationSource != null)
        {
            BaseSyntacticExpression? assignationSource = _activeAssignationSource.ParseToken(token);

            if (assignationSource != null)
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserAssignationIntoNullDestination, $"The parser accepted an assignation into an empty destination expression at {token.Position}");
                }

                if (_isCompilerDirective)
                {
                    throw new AscertainException(AscertainErrorCode.ParserCompilerDirectiveIsNotCallExpression, $"Invalid compiler directive at {token.Position}");
                }
                
                _isCompleted = true;
                return new AssignationSyntacticExpression(_activeExpression.Position, _activeExpression, assignationSource);
            }
            
            return null;
        }
        
        if (_activeParameterListParser != null)
        {
            List<BaseSyntacticExpression>? parameters = _activeParameterListParser.ParseToken(token);
            if (parameters != null)
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserCallOnNullExpression, $"The parser accepted a call for an empty destination expression at {token.Position}");
                }

                _activeExpression = new CallSyntacticExpression(_activeExpression.Position, _activeExpression, parameters, _parsedTypeParameters ?? new());
                _activeParameterListParser = null;
                _parsedTypeParameters = null;
            }

            return null;
        }
        
        if (_activeTypeParameterListParser != null)
        {
            List<BaseSyntacticExpression>? parameters = _activeTypeParameterListParser.ParseToken(token);
            if (parameters != null)
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserCallOnNullExpression, $"The parser accepted a call for an empty destination expression at {token.Position}");
                }

                _activeTypeParameterListParser = null;
                _parsedTypeParameters = parameters;
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
                
                _activeParameterListParser = new ParameterListParser(false);
                return null;
            case "<":

                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserNullStatement, $"The identifier at {token.Position} closes an empty expression.");
                }

                if (_parsedTypeParameters != null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserDuplicateTypeParameterList, $"The identifier at {token.Position} opens a type parameter list, but one was already provided for this call.");
                }
                
                _activeTypeParameterListParser = new ParameterListParser(true);
                return null;
            case "=":
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserAssignationIntoNullStatement, $"The identifier at {token.Position} produces an assignation towards an empty destination expression.");
                }
                
                _activeAssignationSource = new ExpressionParser(false);
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
            case ">":
            case "#":
            case "\"":
                throw new AscertainException(AscertainErrorCode.ParserIllegalOperatorInStatement, $"The identifier at {token.Position} follows an expression without an operator.");
        }

        if (_activeOperator != null)
        {
            if (_activeOperator.Value.Value.Span.Equals(".".AsSpan(), StringComparison.InvariantCulture))
            {
                if (_activeExpression == null)
                {
                    throw new AscertainException(AscertainErrorCode.InternalErrorParserAccessMemberOnNullExpression,
                        $"The identifier at {token.Position} is attempting to define an access member on a null expression.");
                }
                
                if (_isCompilerDirective)
                {
                    throw new AscertainException(AscertainErrorCode.ParserCompilerDirectiveIsNotCallExpression, $"Invalid compiler directive at {token.Position}");
                }

                _activeExpression = new AccessMemberSyntacticExpression(_activeExpression.Position, _activeExpression, token.Value.ToString());
                _activeOperator = null;
            }
            else 
            {
                throw new AscertainException(AscertainErrorCode.InternalErrorParserUnknownOperator, $"The identifier {token.Value.ToString()} at {token.Position} was detected as an operator but it is has no implementation.");
            }
        }
        else if (_activeOperator == null)
        {
            if (_activeExpression != null)
            {
                throw new AscertainException(AscertainErrorCode.ParserIdentifiersNotSeparatedByOperator, $"The identifier {token.Value.ToString()} at {token.Position} follows an expression without an operator.");
            }
            
            _activeExpression = new AccessVariableSyntacticExpression(token.Position, token.Value.ToString(), _isCompilerDirective);
            return null;
        }
        
        return null;
    }
}

public record AccessVariableSyntacticExpression(Position Position, string Name, bool IsCompilerDirective) : BaseSyntacticExpression(Position);

public record CallSyntacticExpression(Position Position, BaseSyntacticExpression Callable, List<BaseSyntacticExpression> Parameters, List<BaseSyntacticExpression> TypeParameters) : BaseSyntacticExpression(Position);

public record AssignationSyntacticExpression(Position Position, BaseSyntacticExpression Destination, BaseSyntacticExpression Source) : BaseSyntacticExpression(Position);

public record AccessMemberSyntacticExpression(Position Position, BaseSyntacticExpression Parent, string MemberName) : BaseSyntacticExpression(Position);

public class ParameterListParser
{
    private readonly bool _isTypeParameterList;
    
    private readonly List<BaseSyntacticExpression> _parameters = new();

    private AccessVariableSyntacticExpression? _currentAccessVariable;
    
    private bool _isCompleted = false;

    public ParameterListParser(bool isTypeParameterList)
    {
        _isTypeParameterList = isTypeParameterList;
    }

    public List<BaseSyntacticExpression>? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        var tokenValue = token.Value.Span;

        switch (tokenValue)
        {
            case ")":
                if (_isTypeParameterList)
                {
                    throw new AscertainException(AscertainErrorCode.ParserTypeParameterListClosedWithParenthesis, $"Type parameter list must be closed with an angled bracket at {token.Position}");
                }

                if (_currentAccessVariable != null)
                {
                    _parameters.Add(_currentAccessVariable);
                    _currentAccessVariable = null;
                }

                _isCompleted = true;
                return _parameters;
            case ">":
                if (!_isTypeParameterList)
                {
                    throw new AscertainException(AscertainErrorCode.ParserParameterListClosedWithAngledBracket, $"Parameter list must be closed with a parenthesis at {token.Position}");
                }

                if (_currentAccessVariable != null)
                {
                    _parameters.Add(_currentAccessVariable);
                    _currentAccessVariable = null;
                }

                _isCompleted = true;
                return _parameters;
            case ",":
                if (_currentAccessVariable != null)
                {
                    _parameters.Add(_currentAccessVariable);
                    _currentAccessVariable = null;
                }
                return null;
        }
        
        if (_currentAccessVariable != null)
        {
            throw new NotImplementedException("Single token parameters supported only");
        }

        _currentAccessVariable = new(token.Position, tokenValue.ToString(), false);
        
        return null;
    }
}