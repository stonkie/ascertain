namespace Ascertain.Compiler;

public enum AscertainErrorCode
{
    LexerIllegalCharacter = 0x00010001,
    
    ParserDuplicateModifier = 0x00020001,
    ParserIllegalCharacterInTypeDefinition = 0x00020002,
    ParserMismatchedClosingScopeAtRootLevel = 0x00020003,
    ParserMissingNameInTypeDefinition = 0x00020004,
    ParserIllegalModifierOnMethod = 0x00020005,
    ParserIllegalModifierOnStatement = 0x00020006,
    ParserTooManyIdentifiersOnMember = 0x00020007,
    ParserModifierAfterTypeOnMember = 0x00020008,
    ParserParametersAppliedOnNonTypeOnMember = 0x00020009,
    ParserParametersAppliedOnNonMethodMember = 0x00020010,
    ParserMissingNameInMemberDefinition = 0x00020011,
    ParserMissingTypeInMemberDefinition = 0x00020012,
    ParserParametersAppliedMoreThanOnceOnMember = 0x00020013,
    
    InternalErrorParserUnknownModifier = 0x10020001,
    InternalErrorParserAttemptingToReuseCompletedTypeParser = 0x10020002, // TODO : Some usages are not for TypeParser
}