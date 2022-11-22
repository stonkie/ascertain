namespace Ascertain.Compiler.Parsing;

public class ParameterDeclarationParser
{
    private bool _isCompleted;

    public IParameterDeclaration? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        var tokenValue = token.Value.Span;
        
        switch (tokenValue)
        {
            case ",":
            case ")":
                _isCompleted = true;
                return new ParameterDeclaration();
        }

        return null;
    }
}