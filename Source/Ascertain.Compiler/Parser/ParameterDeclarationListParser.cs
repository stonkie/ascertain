namespace Ascertain.Compiler.Parser;

public class ParameterDeclarationListParser : IParameterDeclarationListParser
{
    private readonly string _returnTypeName;

    public ParameterDeclarationListParser(string returnTypeName)
    {
        _returnTypeName = returnTypeName;
    }

    public TypeDeclaration? ParseToken(Token token)
    {
        var tokenValue = token.Value.Span;
        
        switch (tokenValue)
        {
            case ",":
                return null;
            case ")":
                return new TypeDeclaration(_returnTypeName, new List<IParameterDeclaration>());
        }

        return null;
    }
}