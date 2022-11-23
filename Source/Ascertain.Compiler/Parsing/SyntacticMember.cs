﻿using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public class SyntacticMember
{
    public Position Position { get; }
    public string Name { get; }
    public Modifier Modifiers { get; }
    public TypeDeclaration TypeDeclaration { get; }
    public BaseExpression Expression { get; }

    public SyntacticMember(Position position, string name, Modifier modifiers, TypeDeclaration typeDeclaration, BaseExpression expression)
    {
        Position = position;
        Name = name;
        Modifiers = modifiers;
        TypeDeclaration = typeDeclaration;
        Expression = expression;
    }
}