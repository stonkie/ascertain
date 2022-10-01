namespace Ascertain.Compiler.Parser;

public interface IMemberParser
{
    IMember? ParseToken(Token token);
}