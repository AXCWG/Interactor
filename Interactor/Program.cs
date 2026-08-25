using System.Diagnostics;
using System.Globalization;
using Wcwidth;

var interactor = new Interactor(); 
interactor.Exec();

class Scene : IEntry
{
    public Scene? Parent { get; set; }
    public ICollection<IEntry> Items { get; } = [];
    public required string Token { get; set; }
}

class Exec : IEntry
{
    public required Action Executor { get; set; }
    public required string Token { get; set; }
}

interface IEntry
{
    public string Token { get; set; }
}


class Interactor
{
    public string Symbol { get; set; } = ">";
    public ICollection<IEntry> RootCommand { get; set; } = [];
    public Scene? Current { get; set; }
    public Interactor()
    {
        RootCommand.Add(new Exec()
        {
            Token = "ls", 
            Executor = ()=>Console.Write("Executed")
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
                Executor = () => Console.Write("listed disks")
            });
        disk.Items.Add(
            new Scene()
            {
                Token = "partitions",
                Parent = disk
            });
        RootCommand.Add(disk);
    }

    public Interactor(string symbol)
    {
        Symbol = symbol; 
    }
    public void Exec()
    {
        while (true)
        {
            Console.Write("{0}{1} ",  Current?.Token, Symbol);
            
            var i = Console.ReadKey(true);
            var bf = new List<string>();
            
            while (i.Key is not ConsoleKey.Enter)
            {
                
                switch (i)
                {
                    case{Key: ConsoleKey.Backspace}:
                        Console.SetCursorPosition(0, Console.CursorTop);
                        
                        for (int j = 0; j < UnicodeCalculator.GetWidth(string.Join("", bf)); j++)
                        {
                            Console.Write(' ');
                        }
                        Console.SetCursorPosition(0, Console.CursorTop);
                        try
                        {
                            bf.RemoveAt(bf.Count -1);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                        Console.Write("{2}{0} {1}", Symbol, string.Join("", bf.ToArray()), Current?.Token);
                        
                        break;
                    default:
                        bf.Add(i.KeyChar.ToString());
                        Console.SetCursorPosition(0, Console.CursorTop);
                        
                        for (int j = 0; j <UnicodeCalculator.GetWidth(string.Join("", bf)); j++)
                        {
                            Console.Write(' ');
                        }
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write("{2}{0} {1}", Symbol, string.Join("", bf.ToArray()), Current?.Token);
                        break;
                }
                // proc
                var e = StringInfo.GetTextElementEnumerator(string.Join("", bf.ToArray()));
                bf.Clear();
                while (e.MoveNext())
                {
                    bf.Add(e.GetTextElement());
                }
                // endproc 
                i = Console.ReadKey(true);
            }
            Console.WriteLine();
            #region Parser
            var finalInput = string.Join("", bf.ToArray()).Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            try
            {
                int currentParseIndex = 0;

                START:
                string? currentToken = finalInput.ElementAtOrDefault(currentParseIndex);
                if (currentToken is null)
                {
                   goto END;
                }

                if (currentToken.ToLowerInvariant() == "exit")
                {
                    if (Current is null)
                    {
                        Console.WriteLine("bye");
                        Environment.Exit(0);
                    }
                    Current = Current?.Parent;
                    goto END;
                }
                var next = (Current?.Items ?? RootCommand).FirstOrDefault(i => i.Token == currentToken) ??
                           throw new NullReferenceException("Bad command");
                if (next is Scene scene)
                {
                    Current = scene;
                    currentParseIndex++;
                    goto START;
                }
                else if (next is Exec exec)
                {
                    exec.Executor.Invoke();
                }

                #endregion
                Debug.WriteLine("{0}, {1}", UnicodeCalculator.GetWidth(string.Join("", bf.ToArray())), bf.Count);
                Console.WriteLine();
                END:

                bf.Clear();
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Bad command");
            }
            
        }
    }
}

static class Test
{
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
}


