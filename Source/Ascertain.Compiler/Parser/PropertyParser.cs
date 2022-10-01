namespace Ascertain.Compiler.Parser;

public class PropertyParser : IMemberParser
{
    private readonly string _activeName;
    private readonly Modifier _activeModifiers;

    public PropertyParser(string activeName, Modifier activeModifiers)
    {
        _activeName = activeName;
        _activeModifiers = activeModifiers;
    }

    public IMember? ParseToken(Token token)
    {
        throw new NotImplementedException();
    }
}