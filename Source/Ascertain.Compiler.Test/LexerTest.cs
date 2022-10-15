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
    public void MultilineTokenization()
    {
        var input = @"public class Program { 
            public static new void(System system) {
                system.GetFileSystem();
            }
        }";
        var inputChars = input.ToCharArray();
        List<Token> expectedTokens = new()
        {
            new (new ReadOnlyMemory<char>(inputChars, 0, 6), new (0, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 7, 5), new (0, 7)),
            new (new ReadOnlyMemory<char>(inputChars, 13, 7), new (0, 13)),
            new (new ReadOnlyMemory<char>(inputChars, 21, 1), new (0, 21)),
            new (new ReadOnlyMemory<char>(inputChars, 37, 6), new (1, 15)),
            new (new ReadOnlyMemory<char>(inputChars, 44, 6), new (1, 22)),
            new (new ReadOnlyMemory<char>(inputChars, 51, 3), new (1, 29)),
            new (new ReadOnlyMemory<char>(inputChars, 55, 4), new (1, 33)),
            new (new ReadOnlyMemory<char>(inputChars, 59, 1), new (1, 37)),
            new (new ReadOnlyMemory<char>(inputChars, 60, 6), new (1, 38)),
            new (new ReadOnlyMemory<char>(inputChars, 67, 6), new (1, 45)),
            new (new ReadOnlyMemory<char>(inputChars, 73, 1), new (1, 51)),
            new (new ReadOnlyMemory<char>(inputChars, 75, 1), new (1, 53)),
            new (new ReadOnlyMemory<char>(inputChars, 94, 6), new (2, 18)),
            new (new ReadOnlyMemory<char>(inputChars, 100, 1), new (2, 24)),
            new (new ReadOnlyMemory<char>(inputChars, 101, 13), new (2, 25)),
            new (new ReadOnlyMemory<char>(inputChars, 114, 1), new (2, 38)),
            new (new ReadOnlyMemory<char>(inputChars, 115, 1), new (2, 39)),
            new (new ReadOnlyMemory<char>(inputChars, 116, 1), new (2, 40)),
            new (new ReadOnlyMemory<char>(inputChars, 131, 1), new (3, 14)),
            new (new ReadOnlyMemory<char>(inputChars, 142, 1), new (4, 10)),
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