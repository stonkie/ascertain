﻿using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

public record ObjectType(QualifiedName Name, Dictionary<string, List<Member>> Members, CompilerPrimitive? Primitive);