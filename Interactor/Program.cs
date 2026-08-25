using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Wcwidth;

var interactor = new Interactor()
{
    Symbol = ">>"
}; 
interactor.Exec();

public class Scene : IEntry
{
    public Scene? Parent { get; set; }
    public ICollection<IEntry> Items { get; } = [];
    public required string Token { get; set; }
}


public class Exec : IEntry
{
    public required Action<string[]> Executor { get; set; }
    public virtual required string Token { get; set; }
    
}



public interface IEntry
{
    public string Token { get; set; }
}

public class Interactor
{
    public string Symbol { get; set; } = ">";
    public ICollection<IEntry> RootCommand { get; set; } = [];
    public Scene? Current { get; set; }
    public Func<string, string> DefaultError { get; set; } = (input)=>$"Bad command: {input}";
    public string SceneSeparator { get; set; } = "/";
    public ICollection<string> History { get; set; } = []; 
    public Interactor()
    {
        RootCommand.Add(new Exec()
        {
            Token = "ls", 
            Executor = (_)=>Console.Write("Executed")
        });
        RootCommand.Add(new Exec()
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
        RootCommand.Add(disk);
    }

    public Interactor(string symbol)
    {
        Symbol = symbol; 
    }
    private string CurrentTokenProcess()
    {
        string path = Current?.Token ?? "";
        Scene? scene = Current?.Parent;
        ACC:
        if (scene is not null)
        {
            path = scene.Token + SceneSeparator + path;
            scene = scene.Parent;
            goto ACC;
        }

        return path;
    }
    private void WriteWithTemplate(string buffer)
    {
       
        Console.Write("{0}{1} {2}", CurrentTokenProcess(), Symbol, buffer);
    }

    private string GiveTemplate(string buffer)
    {
        return $"{CurrentTokenProcess()}{Symbol} {buffer}";
    }
    public void Exec()
    {
        void UnicodeRecreateTermToMem(List<string> bf)
        {
            var e = StringInfo.GetTextElementEnumerator(string.Join("", bf.ToArray()));
            bf.Clear();
            while (e.MoveNext())
            {
                bf.Add(e.GetTextElement());
            }
        }

        void ClearTerminalBuffer(List<string> bf)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int j = 0; j <Console.BufferWidth-1; j++)
            {
                Console.Write(' ');
            }
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        while (true)
        {
            WriteWithTemplate("");
            var i = Console.ReadKey(true);
            var bf = new List<string>();
            int iHist = History.Count ;
            while (i.Key is not ConsoleKey.Enter)
            {
                switch (i)
                {
                    case{Key: ConsoleKey.Backspace}:
                        ClearTerminalBuffer(bf);
                        try
                        {
                            bf.RemoveAt(bf.Count -1);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                        WriteWithTemplate(string.Join("", bf.ToArray()));
                        break;
                    case {Key: ConsoleKey.UpArrow}:
                        iHist--;
                        var current = History.ElementAtOrDefault(iHist);
                        if (current is null)
                        {
                            iHist++;
                            break; 
                        }
                        ClearTerminalBuffer(bf);
                        bf.Clear();
                        bf.Add(current);
                        UnicodeRecreateTermToMem(bf);
                        WriteWithTemplate(string.Join("", bf.ToArray()));
                        // goto skip_proc;
                        break;
                    case {Key: ConsoleKey.DownArrow}:
                        iHist++;
                        current = History.ElementAtOrDefault(iHist);
                        if (current is null)
                        {
                            iHist--;
                            break; 
                        }
                        //important that clear to buffer happens after Clearing screen. 
                        ClearTerminalBuffer(bf);
                        bf.Clear();
                        bf.Add(current);
                        UnicodeRecreateTermToMem(bf);
                        WriteWithTemplate(current);
                        break;
                    
                    
                    default:
                        
                        
                            bf.Add(i.KeyChar.ToString());
                        
                        ClearTerminalBuffer(bf);
                        UnicodeRecreateTermToMem(bf);
                        WriteWithTemplate(string.Join("", bf.ToArray()));
                        // Console.Write("{2}{0} {1}", Symbol, string.Join("", bf.ToArray()), Current?.Token);
                       
                            
                        
                        
                        break;
                }
                // proc
                UnicodeRecreateTermToMem(bf);
                // endproc 
                i = Console.ReadKey(true);
            }
            History.Add(string.Join("", bf.ToArray()));
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
                           throw new NullReferenceException(DefaultError.Invoke(currentToken));
                if (next is Scene scene)
                {
                    Current = scene;
                    currentParseIndex++;
                    goto START;
                }
                if (next is Exec exec)
                {
                    exec.Executor.Invoke(finalInput[(finalInput.IndexOf(currentToken) + 1)..]);
                }
                #endregion
                #if DEBUG
                Debug.WriteLine("{0}, {1}", UnicodeCalculator.GetWidth(string.Join("", bf.ToArray())), bf.Count);
                #endif
                Console.WriteLine();
                END:

                bf.Clear();
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine(e.Message);
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
    public static void Test3()
    {
        var value = "🧑‍🧑‍🧒abcde";
        
        Console.WriteLine(UnicodeCalculator.GetWidth(value));
    }
}


