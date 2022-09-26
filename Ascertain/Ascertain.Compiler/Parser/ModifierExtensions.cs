using System.Buffers;

namespace Ascertain.Compiler.Parser;

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

    public static Modifier AddForStatement(this Modifier current, Modifier newModifier, Position position)
    {
        
        throw new AscertainException(AscertainErrorCode.ParserIllegalModifierOnStatement, $"The modifier {newModifier} is illegal on a statement at {position}");
    }

    public static Modifier? ToModifier(this ReadOnlySpan<char> name)
    {
        switch (name)
        {
            case "class":
                return Modifier.Class;
            case "private":
                return Modifier.Private;
            case "public":
                return Modifier.Public;
        }

        return null;
    }

}