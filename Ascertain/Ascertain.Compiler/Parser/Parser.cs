namespace Ascertain.Compiler.Parser;

public class Parser
{
    private readonly IAsyncEnumerable<Token> _tokens;

    public Parser(IAsyncEnumerable<Token> tokens)
    {
        _tokens = tokens;
    }

    public async IAsyncEnumerable<IObjectType> GetTypes()
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