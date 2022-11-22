using System.Collections.Concurrent;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

public class ProgramAnalyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types;
    private readonly string _programTypeName;
    private Task<ObjectType>? _programType;

    public ProgramAnalyzer(IAsyncEnumerable<SyntacticObjectType> types, string programTypeName)
    {
        _types = types;
        _programTypeName = programTypeName;
    }

    public async Task<ObjectType> GetProgramType()
    {
        _programType ??= AnalyzeProgramType();

        return await _programType;
    }

    private async Task<ObjectType> AnalyzeProgramType()
    {
        using StringReader reader = new StringReader(Compiler.System);
        var systemTypes = new Parser(new Lexer(reader).GetTokens()).GetTypes();
        
        Analyzer analyzer = new(_types.Union(systemTypes), _programTypeName);
        return await analyzer.GetObjectType();
    }
}