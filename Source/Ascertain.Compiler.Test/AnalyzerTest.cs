using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Test;

public class AnalyzerTest
{
    [Fact]
    public void BasicAnalysis()
    {
        var input = @"class Program { 
            public static new void(System system) {
                system.GetFileSystem();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");

        analyser.GetProgramType().GetAwaiter().GetResult();

    }
}