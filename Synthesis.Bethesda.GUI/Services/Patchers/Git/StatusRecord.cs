using System.Windows.Input;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Git;

public record StatusRecord(string Text, bool Processing, bool Blocking, ICommand? Command);