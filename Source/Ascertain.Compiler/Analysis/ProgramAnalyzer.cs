using System.Collections.Concurrent;
using Ascertain.Compiler.Analysis.Deep;
using Ascertain.Compiler.Analysis.Surface;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Analysis;

public class ProgramAnalyzer
{
    private readonly IAsyncEnumerable<SyntacticObjectType> _types;
    private readonly string _programTypeName;

    public ProgramAnalyzer(IAsyncEnumerable<SyntacticObjectType> types, string programTypeName)
    {
        _types = types;
        _programTypeName = programTypeName;
    }

    public async Task<(SurfaceObjectType SoughtType, Func<SurfaceObjectType, ObjectType> TypeResolver)> GetProgramType()
    {
        using StringReader reader = new StringReader(Compiler.System);
        var systemTypes = new Parser(new Lexer(reader).GetTokens()).GetTypes();
        
        SurfaceAnalyzer surfaceAnalyzer = new(_types.Union(systemTypes));
        DeepAnalyzer deepAnalyzer = new DeepAnalyzer(surfaceAnalyzer, _programTypeName);
        
        return await deepAnalyzer.GetSoughtType();
    }
}