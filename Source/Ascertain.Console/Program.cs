
using Ascertain.Compiler;
using Ascertain.Compiler.Parser;

try
{
    if (args.Length != 1)
    {
        throw new Exception($"1 argument is required, but {args.Length} were provided");
    }

    string inputFilePath = args.Single();
    using StreamReader reader = new(File.OpenRead(inputFilePath));

    Lexer lexer = new (reader);
    Parser parser = new(lexer.GetTokens());
    
    await foreach (var obj in parser.GetTypes())
    {
        Console.WriteLine(obj);
    }

}
catch (AscertainException ex)
{
    Console.WriteLine($"Error {ex.ErrorCode} : {ex.ErrorDetails}");
}
catch (Exception ex)
{
    Console.WriteLine($"Internal error {ex}");
}