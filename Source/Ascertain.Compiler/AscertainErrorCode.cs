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
     
    InternalErrorParserUnknownModifier = 0x10020001,
    InternalErrorParserAttemptingToReuseCompletedTypeParser = 0x10020002,
}