using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ParameterDeclarationListParser
{
    private readonly bool _isTypeParameterList;

    private ParameterDeclarationParser? _activeParameterParser;
    private readonly List<SyntacticParameterDeclaration> _accumulatedParameters = new();

    private bool _isCompleted;

    public ParameterDeclarationListParser(bool isTypeParameterList)
    {
        _isTypeParameterList = isTypeParameterList;
    }

    public IReadOnlyList<SyntacticParameterDeclaration>? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        string closingToken = _isTypeParameterList ? ">" : ")";

        if (_activeParameterParser == null)
        {
            var tokenValue = token.Value.Span;
            
            if (tokenValue.Equals(closingToken.AsSpan(), StringComparison.InvariantCulture))
            {
                _isCompleted = true;
                return _accumulatedParameters;
            }

            _activeParameterParser = new ParameterDeclarationParser();
        }
        
        SyntacticParameterDeclaration? parameter = _activeParameterParser.ParseToken(token);

        if (parameter != null)
        {
            _accumulatedParameters.Add(parameter);
        
            var tokenValue = token.Value.Span;
        
            if (tokenValue.Equals(closingToken.AsSpan(), StringComparison.InvariantCulture))
            {
                _isCompleted = true;
                return _accumulatedParameters;
            }
        
            _activeParameterParser = new ParameterDeclarationParser();
        }
        
        return null;
    }
}