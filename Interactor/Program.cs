using System.Diagnostics;
using System.Globalization;
using Wcwidth;

//var interactor = new Interactor(); 
//interactor.Exec();
Test.Test1();

class Scene : IEntry
{
    public List<IEntry> Items { get; set; } = []; 
}

class Exec : IEntry
{
    public required Action Executor { get; set; }
    
}

interface IEntry
{
    
}


class Interactor
{
    public string Symbol { get; set; } = ">";
    public Interactor()
    {
        
    }

    public Interactor(string symbol)
    {
        Symbol = symbol; 
    }
    public void Exec()
    {
        while (true)
        {
            Console.Write("{0} ",  Symbol);
            
            var i = Console.ReadKey(true);
            var bf = new List<char>();
            var width = () =>
            {
                Debug.WriteLine(UnicodeCalculator.GetWidth(new string(bf.ToArray())));
                Debug.WriteLine(bf.Count);
                return UnicodeCalculator.GetWidth(new string(bf.ToArray()));
            };
            while (i.Key is not ConsoleKey.Enter)
            {
                switch (i)
                {
                    case{Key: ConsoleKey.Backspace}:
                        Console.SetCursorPosition(0, Console.CursorTop);
                        
                        for (int j = 0; j < width.Invoke() + 2; j++)
                        {
                            Console.Write(' ');
                        }
                        Console.SetCursorPosition(0, Console.CursorTop);
                        try
                        {
                            bf.RemoveAt(bf.Count - 1);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                        }
                        Console.Write("{0} {1}", Symbol, new string(bf.ToArray()));
                        
                        break;
                    default:
                        bf.Add(i.KeyChar);
                        Console.SetCursorPosition(0, Console.CursorTop);
                        
                        for (int j = 0; j < width.Invoke(); j++)
                        {
                            Console.Write(' ');
                        }
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write("{0} {1}", Symbol, new string(bf.ToArray()));
                        break;
                }
                i = Console.ReadKey(true);
            }
            Console.WriteLine();
            Console.Write(new string(bf.ToArray()));
            Console.WriteLine();
            bf.Clear();
        }
    }
}




