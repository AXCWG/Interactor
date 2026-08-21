using System.Text.Json;

var interact = new Interactor();
var ct = new CancellationTokenSource();

await interact.ExecuteAsync(ct.Token);

union CommandsOrAction(ICollection<Command>, Action);
class Command
{
    // Second level. 
    public bool CanBeInterface { get; set; }
    // First level evaluate evidence
    public CommandsOrAction SubCommands { get; set; } = new List<Command>(); 
}

class Interactor
{
    public ICollection<Command> RootCommands { get; set; } = []; 
    public string Starter { get; set; } = ">";
    /// <summary>
    /// This should never end. 
    /// </summary>
    public void Execute()
    {
        Execute(CancellationToken.None);
    }

    // ReSharper disable once MethodOverloadWithOptionalParameter
    private void Execute(CancellationToken cancellationToken = default)
    {
        var buffer = new List<char>();
        while (true)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                return; 
            }
            Console.Write(Starter + " ");
           while (true)
            {
                var key = Console.ReadKey(false);
                switch (key)
                {
                    case {Key: ConsoleKey.Enter}:
                        var content = new string(buffer.ToArray());
                        buffer.Clear();
                        if (!Normalize(content))
                        {
                            Console.WriteLine();
                            Console.WriteLine("Bad command: {0}", content);
                            goto break_while;
                        }

                        var c = content.Split(' ', StringSplitOptions.TrimEntries);
                        Console.WriteLine(JsonSerializer.Serialize(c));
                        
                        Console.WriteLine();
                        goto break_while;
                    case {Key:ConsoleKey.Backspace}:
                        if (buffer.Count > 0)
                        {
                            try
                            {
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                buffer.RemoveAt(buffer.Count - 1);
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                                Console.Write(" ");
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                            }
                           
                        }
                        else
                        {
                                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            
                        }
                        
                        break;
                    default:
                        buffer.Add(key.KeyChar);
                        break; 
                }
            } break_while: ;
           
            
            
            
            
            
        }
    }

    private bool Normalize(string? str)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            return false; 
        }
        
        return true; 
    }
    public Task ExecuteAsync(CancellationToken ?cancellationToken = null)
    {
        return cancellationToken is null ? Task.Run(() =>
        {
            Execute(CancellationToken.None);
        }) : Task.Run(() =>
        {
            Execute(cancellationToken.Value);
        }, cancellationToken.Value);
    }
}