using UI.Console;

namespace RailroaderRD;

[ConsoleCommand("/echo", "Echo echo echo echo")]
public class EchoCommand : IConsoleCommand
{
    public string Execute(string[] components)
        => string.Join(" ", components);
}
