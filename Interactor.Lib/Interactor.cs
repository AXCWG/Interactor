using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Wcwidth;

namespace Interactor.Lib;

/// <summary>
/// A submenu node in the command tree. Typing its <see cref="Token"/> navigates into it;
/// <c>exit</c> returns to its <see cref="Parent"/> (or quits at the root).
/// </summary>
public class Scene : IEntry
{
    /// <summary>The parent scene; <see langword="null"/> for root-level scenes.</summary>
    public Scene? Parent { get; set; }

    /// <summary>Entries that belong to this scene: child <see cref="Scene"/>s and <see cref="Exec"/> commands.</summary>
    public ICollection<IEntry> Items { get; } = [];

    /// <summary>The input token that enters this scene.</summary>
    public required string Token { get; set; }
}


/// <summary>A command node in the command tree. Typing its <see cref="Token"/> runs <see cref="Executor"/>.</summary>
public class Exec : IEntry
{
    /// <summary>Invoked with the tokens that followed the command token on the input line.</summary>
    public required Action<string[]> Executor { get; set; }

    /// <summary>The input token that runs this command.</summary>
    public virtual required string Token { get; set; }
    
}



/// <summary>A node of the command tree: either a <see cref="Scene"/> or an <see cref="Exec"/>.</summary>
public interface IEntry
{
    /// <summary>The token the user types to select this entry.</summary>
    public string Token { get; set; }
}

/// <summary>
/// A minimal interactive interpreter: reads lines with history recall and caret editing,
/// then parses them against a tree of <see cref="Scene"/>s and <see cref="Exec"/> commands.
/// </summary>
public class Interactor
{
    /// <summary>The prompt symbol shown after the current scene path.</summary>
    public string Symbol { get; set; } = ">";

    /// <summary>The root level of the command tree, used when <see cref="Current"/> is <see langword="null"/>.</summary>
    public ICollection<IEntry> RootCommand { get; set; } = [];

    /// <summary>The scene the user is currently in; <see langword="null"/> means the root.</summary>
    public Scene? Current { get; set; }

    /// <summary>Produces the message printed when an input token matches nothing.</summary>
    public Func<string, string> DefaultError { get; set; } = (input)=>$"Bad command: {input}";

    /// <summary>Separator used when building the scene path shown in the prompt.</summary>
    public string SceneSeparator { get; set; } = "/";

    /// <summary>Input history, most recent entries recalled first via the up arrow.</summary>
    public ICollection<string> History { get; set; } = []; 

    /// <summary>
    /// Creates an interactor with an empty command tree; register <see cref="Scene"/>s and <see cref="Exec"/>
    /// commands on <see cref="RootCommand"/> before calling <see cref="Exec"/>.
    /// </summary>
    public Interactor()
    {
    }

    /// <summary>Creates an interactor with a custom prompt <paramref name="symbol"/> and an empty command tree.</summary>
    public Interactor(string symbol) : this()
    {
        Symbol = symbol; 
    }

    /// <summary>Builds the prompt path of the current scene, e.g. <c>disk/partitions</c>.</summary>
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

    /// <summary>Writes the prompt followed by <paramref name="buffer"/> at the current cursor position.</summary>
    private void WriteWithTemplate(string buffer)
    {
       
        Console.Write("{0}{1} {2}", CurrentTokenProcess(), Symbol, buffer);
    }

    /// <summary>Returns the prompt followed by <paramref name="buffer"/> as a string.</summary>
    private string GiveTemplate(string buffer)
    {
        return $"{CurrentTokenProcess()}{Symbol} {buffer}";
    }

    /// <summary>
    /// Runs the read–parse loop: edits the grapheme buffer with the caret/history keys,
    /// then parses the line against the current scene's entries (<c>exit</c> goes up one level
    /// or quits the app at the root).
    /// </summary>
    public void Exec()
    {
        // Re-splits the joined buffer into grapheme clusters (keeps ZWJ/combining sequences intact).
        void UnicodeRecreateTermToMem(List<string> bf)
        {
            var e = StringInfo.GetTextElementEnumerator(string.Join("", bf.ToArray()));
            bf.Clear();
            while (e.MoveNext())
            {
                bf.Add(e.GetTextElement());
            }
        }

        // Wipes the current console row with spaces and puts the cursor back at its start.
        void ClearTerminalBuffer(List<string> bf)
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            for (int j = 0; j <Console.BufferWidth-1; j++)
            {
                Console.Write(' ');
            }
            Console.SetCursorPosition(0, Console.CursorTop);
        }

        // Redraws the prompt + buffer and parks the terminal cursor at the caret position pos
        // (measured in grapheme clusters, rendered with display widths).
        void RedrawWithCaret(List<string> bf, int pos)
        {
            var joined = string.Join("", bf.ToArray());
            var suffix = string.Join("", bf.Skip(pos).ToArray());
            ClearTerminalBuffer(bf);
            WriteWithTemplate(joined);
            Console.SetCursorPosition(
                Math.Max(0, Console.CursorLeft - UnicodeCalculator.GetWidth(suffix)),
                Console.CursorTop);
        }

        while (true)
        {
            WriteWithTemplate("");
            var i = Console.ReadKey(true);
            var bf = new List<string>();
            int iHist = History.Count ;
            int pos = 0; // caret position in grapheme clusters (←/→)
            while (i.Key is not ConsoleKey.Enter)
            {
                switch (i)
                {
                    case{Key: ConsoleKey.Backspace}:
                        if (pos > 0)
                        {
                            bf.RemoveAt(pos - 1);
                            pos--;
                        }
                        RedrawWithCaret(bf, pos);
                        break;
                    case {Key: ConsoleKey.LeftArrow}:
                        if (pos > 0)
                        {
                            pos--;
                            RedrawWithCaret(bf, pos);
                        }
                        break;
                    case {Key: ConsoleKey.RightArrow}:
                        if (pos < bf.Count)
                        {
                            pos++;
                            RedrawWithCaret(bf, pos);
                        }
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
                        pos = bf.Count; // caret snaps to end
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
                        pos = bf.Count; // caret snaps to end
                        break;
                    
                    
                    default:
                        
                        
                            if (i.KeyChar == '\0')
                            {
                                break;
                            }
                            bf.Insert(Math.Min(pos, bf.Count), i.KeyChar.ToString());
                            pos = Math.Min(pos + 1, bf.Count);
                        
                        UnicodeRecreateTermToMem(bf);
                        pos = Math.Min(pos, bf.Count);
                        RedrawWithCaret(bf, pos);
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