namespace Ascertain.Compiler.Parser;

public class MethodParser : IMemberParser
{
    private readonly string _methodName;
    private readonly Modifier _methodModifiers;
    
    private int depth = 0;

    public MethodParser(string methodName, Modifier methodModifiers)
    {
        _methodName = methodName;
        _methodModifiers = methodModifiers;
    }

    public IMember? ParseToken(Token token)
    {
        if (depth < 0)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        var tokenValue = token.Value.Span;
        
        switch (tokenValue)
        {
            case "=":
                // TODO
                return null;
            case "}":
                depth--;
                if (depth < 0)
                {
                    return new Method();   
                }
                break;
            case "{":
                depth++;
                break;
            case ";":
            case "(":
            case ")":
            case ".":
                break;
        }

        return null;
    }
}