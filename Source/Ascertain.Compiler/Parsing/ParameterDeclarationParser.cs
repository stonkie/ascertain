using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class ParameterDeclarationParser
{
    private bool _isCompleted;

    private SyntacticTypeReference? _activeTypeReference = null;
    private string? _activeName = null;
    private readonly TypeReferenceParser _typeReferenceParser = new();

    public SyntacticParameterDeclaration? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        if (_activeTypeReference == null)
        {
            SyntacticTypeReference? typeReference = _typeReferenceParser.ParseToken(token);

            if (typeReference != null)
            {
                _activeTypeReference = typeReference;
            }

            return null;
        }

        var tokenValue = token.Value.Span;
        
        switch (tokenValue)
        {
            case "{":
            case "=":
            case "}":
            case "(":
            case ";":
            case ".":
                throw new AscertainException(AscertainErrorCode.ParserIllegalTokenInParameterDeclaration, $"Token {tokenValue} at {token.Position} is illegal in a parameter declaration");
            case ",":
            case ")":
                _isCompleted = true;

                if (_activeTypeReference == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserTypeMissingInParameterDeclaration, $"Token {tokenValue} at {token.Position} closes a parameter declaration without a type");
                }

                if (_activeName == null)
                {
                    throw new AscertainException(AscertainErrorCode.ParserNameMissingInParameterDeclaration, $"Token {tokenValue} at {token.Position} closes a parameter declaration without a name");
                }
                
                return new SyntacticParameterDeclaration(_activeName, _activeTypeReference);
        }

        if (_activeName != null)
        {
            throw new AscertainException(AscertainErrorCode.ParserTooManyIdentifiersInParameterDeclaration, $"Token {tokenValue} at {token.Position} was found after parameter name {_activeName}");
        }

        _activeName = tokenValue.ToString();

        return null;
    }
}