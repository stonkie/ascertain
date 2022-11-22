namespace Ascertain.Compiler.Lexing;

public record struct Token(ReadOnlyMemory<char> Value, Position Position);