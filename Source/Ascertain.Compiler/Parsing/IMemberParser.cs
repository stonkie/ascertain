namespace Ascertain.Compiler.Parsing;

public interface IMemberParser
{
    SyntacticMember? ParseToken(Token token);
}