namespace Ascertain.Compiler.Parser;

internal interface IParameterDeclarationListParser
{
    TypeDeclaration? ParseToken(Token token);
}