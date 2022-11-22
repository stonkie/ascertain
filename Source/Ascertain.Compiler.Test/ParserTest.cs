using System.Globalization;
using Ascertain.Compiler.Lexing;
using Ascertain.Compiler.Parsing;

namespace Ascertain.Compiler.Test;

public class ParserTest
{
    [Fact]
    public void BasicParser()
    {
        var input = @"class Program { 
            public static New Program(System system) {
                system.GetFileSystem();
            }
        }";

        using var reader = new StringReader(input);
        Lexer lexer = new(reader);

        var tokens = lexer.GetTokens();
        var objects = new Parsing.Parser(tokens).GetTypes().ToListAsync();
        var programObject = objects.GetAwaiter().GetResult().Single();

        Assert.Equal("Program", programObject.Name);
        Assert.Equal(Modifier.Class, programObject.Modifiers);

        Assert.Single(programObject.Members);

        var member = programObject.Members.Single();
        Assert.Equal("new", member.Name);
        Assert.Equal(Modifier.Public | Modifier.Static, member.Modifiers);

        // Method content
        {
            var methodScope = Assert.IsType<Scope>(member.Expression);
            Assert.Equal(1, methodScope.Statements.Count);

            var callExpression = Assert.IsType<CallExpression>(methodScope.Statements.Single());
            Assert.Empty(callExpression.Parameters);

            var accessMemberExpression = Assert.IsType<AccessMemberExpression>(callExpression.Callable);
            Assert.Equal("GetFileSystem", accessMemberExpression.Member.Value.Span.ToString());

            var variableExpression = Assert.IsType<VariableExpression>(accessMemberExpression.Parent);
            Assert.Equal("system", variableExpression.Token.Value.Span.ToString());
        }

        var methodType = Assert.IsType<SyntacticMember>(member).TypeDeclaration;
        Assert.Equal("Program", methodType.ReturnTypeName);
        Assert.NotNull(methodType.ParameterDeclarations);
        Assert.Equal(1, methodType.ParameterDeclarations!.Count); // TODO : Expand on this
    }
}