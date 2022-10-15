namespace Ascertain.Compiler.Parser;

internal interface IParameterDeclarationListParser
{
    List<IParameterDeclaration>? ParseToken(Token token);
}