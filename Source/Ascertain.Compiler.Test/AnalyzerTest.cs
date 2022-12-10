using System.Reflection;
using Ascertain.Compiler.Analysis;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Test;

public class AnalyzerTest
{
    [Fact]
    public void BasicAnalysis()
    {
        var input = @"class Program {
            public static New Program(System system) {
                system.GetFileSystem();

                new ();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);
        Parser parser = new(lexer.GetTokens());
        ProgramAnalyzer analyser = new(parser.GetTypes(), "Program");

        var program = analyser.GetProgramType().GetAwaiter().GetResult();

        var constructor = program.SoughtType.Members["New"].Single();

        Assert.True(constructor.IsPublic);
        Assert.True(constructor.IsStatic);
        Assert.Equal("New", constructor.Name);
    }
}