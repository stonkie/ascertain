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
            new (new ReadOnlyMemory<char>(inputChars, 5, 6), new (5, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 11, 1), new (11, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 12, 2), new (12, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 14, 1), new (14, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 15, 9), new (15, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 24, 1), new (24, 0)),
        };
        
        using StringReader reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens().ToListAsync().GetAwaiter().GetResult();

        var expectedAsStrings = expectedTokens.Select(t => t.Value.ToString()).ToList();
        var resultAsStrings = tokens.Select(t => t.Value.ToString()).ToList();
        
        Assert.Equal(expectedAsStrings, resultAsStrings);
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