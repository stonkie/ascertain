namespace Ascertain.Compiler.Parser;

public interface IStatementParser
{
    IExpression? ParseToken(Token token);
}