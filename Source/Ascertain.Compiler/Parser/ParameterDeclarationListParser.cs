namespace Ascertain.Compiler.Parser;

public class ParameterDeclarationListParser : IParameterDeclarationListParser
{
    private readonly string _returnTypeName;

    private ParameterDeclarationParser _activeParameterParser;
    private readonly List<IParameterDeclaration> _accumulatedParameters = new();

    private bool _isCompleted;

    public ParameterDeclarationListParser(string returnTypeName)
    {
        _returnTypeName = returnTypeName;
        _activeParameterParser = new ParameterDeclarationParser();
    }

    public TypeDeclaration? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        IParameterDeclaration? parameter = _activeParameterParser.ParseToken(token);

        if (parameter != null)
        {
            _accumulatedParameters.Add(parameter);
            
            var tokenValue = token.Value.Span;
            
            if (tokenValue.Equals(")".AsSpan(), StringComparison.InvariantCulture))
            {
                _isCompleted = true;
                return new TypeDeclaration(_returnTypeName, _accumulatedParameters);
            }
            
            _activeParameterParser = new ParameterDeclarationParser();
        }
        
        return null;
    }
}