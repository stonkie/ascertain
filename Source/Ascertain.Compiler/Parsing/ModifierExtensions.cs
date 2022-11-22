using System.Buffers;
using Ascertain.Compiler.Lexing;

namespace Ascertain.Compiler.Parsing;

public static class ModifierExtensions
{
    public static Modifier AddForType(this Modifier current, Modifier newModifier, Position position)
    {
        if (current.HasFlag(newModifier))
        {
            throw new AscertainException(AscertainErrorCode.ParserDuplicateModifier, $"The modifier {newModifier} is applied more than once at {position}");
        }

        return current | newModifier;
    }
    
    public static Modifier AddForMethod(this Modifier current, Modifier newModifier, Position position)
    {
        if (newModifier == Modifier.Class)
        {
            throw new AscertainException(AscertainErrorCode.ParserIllegalModifierOnMethod, $"The modifier {newModifier} is illegal on a method at {position}");
        }
        
        if (current.HasFlag(newModifier))
        {
            throw new AscertainException(AscertainErrorCode.ParserDuplicateModifier, $"The modifier {newModifier} is applied more than once at {position}");
        }

        return current | newModifier;
    }

    public static Modifier? ToModifier(this ReadOnlySpan<char> name)
    {
        switch (name)
        {
            case "class":
                return Modifier.Class;
            case "public":
                return Modifier.Public;
            case "static":
                return Modifier.Static;
        }

        return null;
    }

}