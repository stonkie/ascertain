namespace Ascertain.Compiler.Test;

public class LexerTest
{
    [Fact]
    public void BasicTokenization()
    {
        var input = "This should.be(tokenized)";
        var inputChars = input.ToCharArray();
        List<Token> expectedTokens = new()
        {
            new (new ReadOnlyMemory<char>(inputChars, 0, 4), new (0, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 5, 6), new (0, 5)),
            new (new ReadOnlyMemory<char>(inputChars, 11, 1), new (0, 11)),
            new (new ReadOnlyMemory<char>(inputChars, 12, 2), new (0, 12)),
            new (new ReadOnlyMemory<char>(inputChars, 14, 1), new (0, 14)),
            new (new ReadOnlyMemory<char>(inputChars, 15, 9), new (0, 15)),
            new (new ReadOnlyMemory<char>(inputChars, 24, 1), new (0, 24)),
        };
        
        using StringReader reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens().ToListAsync().GetAwaiter().GetResult();

        Assert.Equal(expectedTokens.Count, tokens.Count);

        for (var index = 0; index < expectedTokens.Count; index++)
        {
            var expectedToken = expectedTokens[index];
            var token = tokens[index];
            
            Assert.Equal(expectedToken.Value.ToString(), token.Value.ToString());
            Assert.Equal(expectedToken.Position, token.Position);
        }
    }

    [Fact]
    public void BasicParser()
    {
        var input = @"public class Program { 
            public static new void(System system) {
                system.GetFileSystem();
            }
        }";
        
        using StringReader reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens();
        var objects = new Parser.Parser(tokens).GetTypes().ToListAsync();
        var programObject = objects.GetAwaiter().GetResult().Single();

        Assert.Equal("Program", programObject.Name);
    }
}