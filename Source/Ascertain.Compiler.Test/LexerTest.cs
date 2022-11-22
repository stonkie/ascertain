using Ascertain.Compiler.Lexing;

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
        var input = @"class Program { 
            public static New void(System system) {
                system.GetFileSystem();
            }
        }";
        var inputChars = input.ToCharArray();
        List<Token> expectedTokens = new()
        {
            new (new ReadOnlyMemory<char>(inputChars, 0, 5), new (0, 0)),
            new (new ReadOnlyMemory<char>(inputChars, 6, 7), new (0, 6)),
            new (new ReadOnlyMemory<char>(inputChars, 14, 1), new (0, 14)),
            new (new ReadOnlyMemory<char>(inputChars, 30, 6), new (1, 15)),
            new (new ReadOnlyMemory<char>(inputChars, 37, 6), new (1, 22)),
            new (new ReadOnlyMemory<char>(inputChars, 44, 3), new (1, 29)),
            new (new ReadOnlyMemory<char>(inputChars, 48, 4), new (1, 33)),
            new (new ReadOnlyMemory<char>(inputChars, 52, 1), new (1, 37)),
            new (new ReadOnlyMemory<char>(inputChars, 53, 6), new (1, 38)),
            new (new ReadOnlyMemory<char>(inputChars, 60, 6), new (1, 45)),
            new (new ReadOnlyMemory<char>(inputChars, 66, 1), new (1, 51)),
            new (new ReadOnlyMemory<char>(inputChars, 68, 1), new (1, 53)),
            new (new ReadOnlyMemory<char>(inputChars, 87, 6), new (2, 18)),
            new (new ReadOnlyMemory<char>(inputChars, 93, 1), new (2, 24)),
            new (new ReadOnlyMemory<char>(inputChars, 94, 13), new (2, 25)),
            new (new ReadOnlyMemory<char>(inputChars, 107, 1), new (2, 38)),
            new (new ReadOnlyMemory<char>(inputChars, 108, 1), new (2, 39)),
            new (new ReadOnlyMemory<char>(inputChars, 109, 1), new (2, 40)),
            new (new ReadOnlyMemory<char>(inputChars, 124, 1), new (3, 14)),
            new (new ReadOnlyMemory<char>(inputChars, 135, 1), new (4, 10)),
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
}