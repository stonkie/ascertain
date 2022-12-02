using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class MethodParser : IMemberParser
{
    private readonly string _methodName;
    private readonly Modifier _methodModifiers;
    private readonly TypeDeclaration _typeDeclaration;

    private readonly ScopeParser _activeScopeParser;
    
    private bool _isCompleted;
    
    public MethodParser(string methodName, Modifier methodModifiers, TypeDeclaration typeDeclaration)
    {
        _methodName = methodName;
        _methodModifiers = methodModifiers;
        _typeDeclaration = typeDeclaration;
        _activeScopeParser = new ScopeParser();
    }

    public SyntacticMember? ParseToken(Token token)
    {
        if (_isCompleted)
        {
            throw new AscertainException(AscertainErrorCode.InternalErrorParserAttemptingToReuseCompletedTypeParser, $"The parser was already completed and cannot be reused for token at {token.Position}");
        }

        var scope = _activeScopeParser.ParseScopeToken(token);

        if (scope != null)
        {
            _isCompleted = true;
            return new SyntacticMember(token.Position, _methodName, _methodModifiers, _typeDeclaration, scope);
        }
        
        return null;
    }
}