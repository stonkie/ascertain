﻿namespace Ascertain.Compiler.Parsing;

public interface IStatementParser
{
    IExpression? ParseToken(Token token);
}