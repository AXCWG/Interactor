using System.Collections;
using System.Text;
using System.Text.Json;

var interact = new Interactor();

 interact.Execute();

union  CommandsOrAction(IList<Command>, Action);
class Command
{
    public Command Parent { get; set; }
    // Second level. 
    public bool CanBeInterface { get; set; }
    // First level evaluate evidence
    public CommandsOrAction SubCommandsOrAction { get; set; } = new List<Command>(); 
    public required string Token { get; set; }
    
}

class Interactor
{
    public IList<Command> RootCommands { get; set; } = [new Command
    {
        Token = "ls", SubCommandsOrAction = new Action(() =>
        {
            Console.WriteLine();
            Console.Write("lsslslsl");
        })
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
                        
                        var @out = new string(buffer.ToArray()).Split(' ',
                            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        var idx = 1;
                        var command = RootCommands.FirstOrDefault(i=>i.Token == @out[0]);
                        if (command is not null)
                        {
                            var sm = new ExecutorStateMachine(command);
                            if (@out.Length != idx)
                            {
                                while (sm.HasNext(@out[idx]))
                                {
                                    sm.NextOrExec(@out[idx]);
                                    idx++;
                                }
                            }
                            else
                            {
                                sm.NextOrExec(@out[0]);
                            }
                            Console.WriteLine();
                            buffer.Clear();
                            break; 
                        }
                        Console.WriteLine();
                        Console.Write("Bad command");
                        
                       
                    }
                    else
                    {
                        fail:
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

static class Ext
{
    public delegate bool CurrentWithNextDelegate<in T>(T current, T next);
    extension<T>(IList<T> collection)
    {
        public bool CurrentWithNext(CurrentWithNextDelegate<T> predicate)
        {
            int index = 0;
            while (index < collection.Count)
            {
                if (!predicate(collection[index], collection[index + 1]))
                {
                    return false;
                }

                index += 2;
            }

            return true;
        }
    }
}

class ExecutorStateMachine
{
    private Command Executing { get; set; }
    

    public ExecutorStateMachine(Command build)
    {
        Executing = build;
    }

    public void NextOrExec(string? content)
    {
        
        if (Executing.SubCommandsOrAction is IList<Command>)
        {
            Executing = ((IList<Command>)Executing.SubCommandsOrAction.Value).FirstOrDefault(i => i.Token == content);
            if (Executing is null)
            {
                Console.Write("Bad command");
            }
        }
        else
        {
            ((Action)Executing.SubCommandsOrAction.Value).Invoke();
        }
    }

    public bool HasNext(string token)
    {
        if (Executing.SubCommandsOrAction is IList<Command> )
        {
            if ((
                    (IList<Command>)Executing.SubCommandsOrAction.Value).Count != 0 &&
                ((IList<Command>)Executing.SubCommandsOrAction.Value).Any(i=>i.Token == token))
            {
                return true;
            }

        }

        return false;
    }
}