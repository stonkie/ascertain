using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ExpressionParser : IStatementParser
{
    private Token? _activeOperator = null;
    private BaseSyntacticExpression? _activeExpression = null;

    private ExpressionParser? _activeAssignationSource = null;
    private ParameterListParser? _activeParameterListParser = null; 
    
    private bool _isCompleted;

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

                _activeExpression = new CallSyntacticExpression(_activeExpression.Position, _activeExpression, parameters);
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

                _activeExpression = new AccessMemberSyntacticExpression(_activeExpression.Position, _activeExpression, token.Value.ToString());
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
            
            _activeExpression = new AccessVariableSyntacticExpression(token.Position, token.Value.ToString());
            return null;
        }
        
        return null;
    }
}

public record AccessVariableSyntacticExpression(Position Position, string Name) : BaseSyntacticExpression(Position);

public record CallSyntacticExpression(Position Position, BaseSyntacticExpression Callable, List<BaseSyntacticExpression> Parameters) : BaseSyntacticExpression(Position);

public record AssignationSyntacticExpression(Position Position, BaseSyntacticExpression Destination, BaseSyntacticExpression Source) : BaseSyntacticExpression(Position);

public record AccessMemberSyntacticExpression(Position Position, BaseSyntacticExpression Parent, string MemberName) : BaseSyntacticExpression(Position);

public class ParameterListParser
{
    private AccessVariableSyntacticExpression? _accessVariable = null;
    
    public List<BaseSyntacticExpression>? ParseToken(Token token)
    {
        var tokenValue = token.Value.Span;

        if (tokenValue.Equals(")".AsSpan(), StringComparison.InvariantCulture))
        {
            // TODO : Complete parameter list parsing

            if (_accessVariable != null)
            {
                return new List<BaseSyntacticExpression>() { _accessVariable };    
            }
            else
            {
                return new List<BaseSyntacticExpression>();
            }
        }

        if (_accessVariable != null)
        {
            throw new NotImplementedException("Single token parameters supported only");
        }

        _accessVariable = new(token.Position, tokenValue.ToString());
        
        return null;
    }
}