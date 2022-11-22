using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class Parser
{
    private readonly IAsyncEnumerable<Token> _tokens;

    public Parser(IAsyncEnumerable<Token> tokens)
    {
        _tokens = tokens;
    }

    public async IAsyncEnumerable<SyntacticObjectType> GetTypes()
    {
        FileParser parser = new();
        
        await foreach (var token in _tokens)
        {
            var rootType = parser.ParseToken(token);

            if (rootType != null)
            {
                yield return rootType;
            }
        }
    }

}