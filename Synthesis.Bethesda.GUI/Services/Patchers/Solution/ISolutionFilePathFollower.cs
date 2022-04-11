using System;
using Noggog;

namespace Synthesis.Bethesda.GUI.Services.Patchers.Solution;

public interface ISolutionFilePathFollower
{
    IObservable<FilePath> Path { get; }
}