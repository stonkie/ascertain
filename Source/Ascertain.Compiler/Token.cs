namespace Ascertain.Compiler;

public record struct Token(ReadOnlyMemory<char> Value, Position Position);