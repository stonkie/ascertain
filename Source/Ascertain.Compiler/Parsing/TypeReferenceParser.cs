using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class TypeReferenceParser
{
    private bool _isCompleted;

    public SyntacticTypeReference? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }
        
        // TODO : Implement templated type references (and possibly function reference?) 

        _isCompleted = true;
        return new SyntacticTypeReference(token.Position, token.Value.ToString());
    }
}