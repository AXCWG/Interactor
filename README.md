# Interactor

A minimal, interactive command interpreter (REPL) for .NET. Commands are organized as a tree of
**scenes** (submenus) and **executables**, input is a small Unicode-aware line editor with caret
movement and history, and the prompt shows the current scene path.

```
>> disk
disk>> list
listed disks
disk>> partitions
disk/partitions>> exit
disk>> exit
>> exit
bye
```

## Features

- **Command tree** — `Scene` (submenu) and `Exec` (command) nodes sharing the `IEntry` interface
- **Scene path prompt** — e.g. `disk/partitions>>` (path separator and symbol configurable)
- **Line editor** — `←`/`→` move the caret, `Backspace` deletes the grapheme before the caret,
  typing inserts at the caret, `↑`/`↓` recall history (newest first)
- **Unicode aware** — the buffer is split into grapheme clusters (ZWJ emoji, combining marks, CJK)
  and the caret is positioned by display width via the [Wcwidth](https://www.nuget.org/packages/Wcwidth)
  package
- **Scene navigation** — `exit` goes up one scene level; at the root it quits the app

## Usage

```bash
dotnet run --project Interactor
```

The demo registers a `ls` and `getParam` command plus a `disk` scene (`list` command and a nested
`partitions` scene) at the root.

### Keys

| Key          | Action                                  |
|--------------|-----------------------------------------|
| `←` / `→`    | move the caret through the line         |
| `Backspace`  | delete the grapheme before the caret    |
| `↑` / `↓`    | recall older / newer history            |
| `Enter`      | submit the line and run it              |

Supported shortcuts: `exit` (leave the current scene, or quit at the root).

## Adding commands

Commands and scenes are registered on `RootCommand` before `Exec()` is called. Use the parameterless
constructor and populate the tree yourself:

```csharp
using Interactor.Lib;

var interactor = new Interactor { Symbol = ">>" };

interactor.RootCommand.Add(new Exec
{
    Token = "hello",
    Executor = args => Console.WriteLine($"Hello, {string.Join(' ', args)}")
});

var tools = new Scene { Token = "tools" };
tools.Items.Add(new Exec
{
    Token = "now",
    Executor = _ => Console.WriteLine(DateTime.Now.ToString())
});
interactor.RootCommand.Add(tools);

interactor.Exec();
```

The `Executor` receives the tokens that followed the command token, split on spaces
(e.g. `hello world foo` → `["world", "foo"]`).

## Layout

```
Interactor.slnx
├── Interactor.Lib          # class library: IEntry / Scene / Exec / Interactor REPL
│   └── Interactor.cs
└── Interactor              # console host: registers the demo commands and runs
    └── Program.cs
```

Requires .NET 11 SDK. Build with `dotnet build`.

## Notes & limitations

- Each keystroke redraws the line by wiping the current console row; lines wider than the console
  buffer are not wrapped cleanly and may leave residue.
- Parameter matching is a simple space split — there is no quote handling or argument parser.
- No default commands are built in; the `Interactor()` constructor starts with an empty command
  tree that you populate via `RootCommand`.
