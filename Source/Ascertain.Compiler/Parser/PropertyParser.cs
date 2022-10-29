﻿namespace Ascertain.Compiler.Parser;

public class PropertyParser : IMemberParser
{
    private TypeDeclaration _memberType;
    private readonly string _activeName;
    private readonly Modifier _activeModifiers;

    public PropertyParser(string activeName, Modifier activeModifiers, TypeDeclaration memberType)
    {
        _memberType = memberType;
        _activeName = activeName;
        _activeModifiers = activeModifiers;
    }

    public IMember? ParseToken(Token token)
    {
        throw new NotImplementedException();
    }
}