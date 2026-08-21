using System.Text;
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
    public required string Token { get; set; }
    
}

class Interactor
{
    public ICollection<Command> RootCommands { get; set; } = [new Command
    {
        Token = "ls"
    }]; 
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
            Refresher();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                return; 
            }

            void Refresher()
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                for (int i = 0; i < 2 + buffer.Count; i++)
                {
                    Console.Write(" ");
                }
                Console.SetCursorPosition(0, Console.CursorTop);
                
                Console.Write(Starter + " " + new string(buffer.ToArray()));
            }

            var consoleKeyInfo = Console.ReadKey(true);
            switch (consoleKeyInfo)
            {
                case {Key: ConsoleKey.Enter}:
                    if (Check(new string(buffer.ToArray())))
                    {
                        void CommandParse(string[] command, int depth)
                        {
                        }
                        var @out = new string(buffer.ToArray()).Split(' ',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        
                        Console.WriteLine();
                        Console.Write(JsonSerializer.Serialize(@out));
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.Write("Bad command");
                        
                    }
                    buffer.Clear();
                    Console.WriteLine();
                    break; 
                case {Key: ConsoleKey.Backspace}:
                    if (buffer.Count > 0)
                    {
                        buffer.RemoveAt(buffer.Count - 1);
                    }
                    break;
                default:
                    buffer.Add(consoleKeyInfo.KeyChar);
                    break; 
            }
            
            
        }
    }

    private bool Check(string? str)
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