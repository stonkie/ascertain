namespace Ascertain.Compiler.Parsing;

public interface IMemberParser
{
    Member? ParseToken(Token token);
}