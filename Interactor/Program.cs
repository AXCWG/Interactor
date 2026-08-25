using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Interactor.Lib;
using Wcwidth;
var interactor = new Interactor.Lib.Interactor()
{
    Symbol = ">>"
}; 
interactor.RootCommand.Add(new Exec()
{
    Token = "ls", 
    Executor = (_)=>Console.Write("Executed")
});
interactor.RootCommand.Add(new Exec()
{
    Token = "getParam",
    Executor = (p)=>Console.Write(JsonSerializer.Serialize(p))
});
var disk = new Scene()
{
    Parent = null,
    Token = "disk",
};
disk.Items.Add(
    new Exec()
    {
        Token = "list",
        Executor = (_) => Console.Write("listed disks")
    });
disk.Items.Add(
    new Scene()
    {
        Token = "partitions",
        Parent = disk
    });
interactor.RootCommand.Add(disk);

interactor.Exec();



/// <summary>Small experiments with grapheme clusters, runes, and display widths.</summary>
static class Test
{
    /// <summary>Prints the number of grapheme clusters in a ZWJ family emoji.</summary>
    public static void Test1()
    {
        var value = "👨‍👩‍👧‍👦";
        var graphemes = new List<string>(); 
        var e = StringInfo.GetTextElementEnumerator(value);
        while (e.MoveNext())
        {
            graphemes.Add(e.GetTextElement());
        }
        Console.WriteLine(graphemes.Count);
    }

    /// <summary>Prints the number of runes in a string mixing ZWJ emoji and CJK text.</summary>
    public static void Test2()
    {
        var value = "🧑‍🧑‍🧒写";
        int length = 0;
        foreach (var enumerateRune in value.EnumerateRunes())
        {
            length++;
        }
        Console.WriteLine(length);
    }
    /// <summary>Prints the display width of a mixed emoji/Latin string via the Wcwidth package.</summary>
    public static void Test3()
    {
        var value = "🧑‍🧑‍🧒abcde";
        
        Console.WriteLine(UnicodeCalculator.GetWidth(value));
    }
}


