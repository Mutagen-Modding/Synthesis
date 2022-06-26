namespace Synthesis.Bethesda.Execution.Patchers.Common;

public interface IPatcherNameProvider
{
    public string Name { get; }
}

public interface IPatcherNicknameProvider
{
    public string Nickname { get; set; }
}