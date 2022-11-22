using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

internal interface IParameterDeclarationListParser
{
    TypeDeclaration? ParseToken(Token token);
}