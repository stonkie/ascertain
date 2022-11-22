﻿namespace Ascertain.Compiler.Lexing;

public class Lexer
{
    private readonly TextReader _reader;
    private ReadOnlyMemory<char> _buffer = new();
    private Position _position = new(0, 0);
    
    public Lexer(TextReader reader)
    {
        _reader = reader;
    }

    public async IAsyncEnumerable<Token> GetTokens()
    {
        var buffer = new char[1024 * 8];
        
        int length = await _reader.ReadBlockAsync(buffer);
        
        while (length > 0)
        {
            _buffer = buffer[..length];
            var tokens = ConsumeTokens();

            foreach (var token in tokens)
            {
                yield return token;
            }
            
            _buffer.CopyTo(buffer);
            
            length = await _reader.ReadBlockAsync(buffer, _buffer.Length, buffer.Length - _buffer.Length);
        }

        if (!_buffer.IsEmpty)
        {
            yield return new Token(_buffer, _position);
        }
    }

    private IEnumerable<Token> ConsumeTokens()
    {
        List<Token> tokens = new();
        var input = _buffer.Span;

        var previousCharType = CharType.WhiteSpace;
        int tokenStart = 0;
        
        for (int i = 0; i < input.Length; i++)
        {
            var charType = GetCharType(input[i]);
            
            // Insert a token cut
            if (charType != previousCharType || previousCharType.IsOneTokenPerChar())
            {
                if (previousCharType != CharType.WhiteSpace)
                {
                    tokens.Add(new Token(_buffer.Slice(tokenStart, i - tokenStart), _position));
                }

                _position = _position with { CharIndex = _position.CharIndex + i - tokenStart };
                tokenStart = i;
            }
            
            if (input[i] == '\n')
            {
                _position = _position with
                {
                    LineIndex = _position.LineIndex + 1, 
                    CharIndex = 0
                };
            }

            previousCharType = charType;
        }

        _buffer = _buffer.Slice(tokenStart);

        return tokens;
    }
    
    private CharType GetCharType(char c)
    {
        if (char.IsWhiteSpace(c))
        {
            return CharType.WhiteSpace;
        }
        else if (char.IsLetterOrDigit(c))
        {
            return CharType.Identifier;
        }
        else
        {
            switch (c)
            {
                case '{':
                case '}':
                case '(':
                case ')':
                    return CharType.Grouper;
                case '.':
                case ';':
                case '=':
                case '#':
                    return CharType.Operator;
                default:
                    throw new AscertainException(AscertainErrorCode.LexerIllegalCharacter, $"illegal character {c} at {_position}");
            }
        }
    }
}