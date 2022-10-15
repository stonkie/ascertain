namespace Ascertain.Compiler.Parser;

public class ParameterDeclarationListParser : IParameterDeclarationListParser
{
    public List<IParameterDeclaration>? ParseToken(Token token)
    {
        var tokenValue = token.Value.Span;
        
        switch (tokenValue)
        {
            case "{":
            case "=":
            case "}":
            case "(":
            case ";":
            case ".":
                return null;
            case ")":
                return new List<IParameterDeclaration>();
        }

        return null;
    }
}