using FluentAssertions;
using Noggog;
using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;
using Xunit;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet;

public class IsApplicableErrorLineTests
{
    [Theory, SynthAutoData]
    public void Typical(IsApplicableErrorLine sut)
    {
        var rawMessage = @"Unknown Error: Microsoft (R) Build Engine version 16.9.0+57a23d249 for .NET
Copyright (C) Microsoft Corporation. All rights reserved.

  Determining projects to restore...
  Restored C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj (in 481 ms).
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\WeaponAnalyer.cs(226,39): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(220,39): warning CS8632: The annotation for nullable reference types should only be used in code within a '#nullable' annotations context. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(14,26): warning CS0618: 'SynthesisPipeline.Run(string[], RunPreferences?)' is obsolete: 'Using SetTypicalOpen is the new preferred API for supplying RunDefaultPatcher preferences' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(158,50): error CS0266: Cannot implicitly convert type 'Mutagen.Bethesda.Skyrim.LeveledItem' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'. An explicit conversion exists (are you missing a cast?) [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(21,25): warning CA1416: This call site is reachable on all platforms. 'RunDefaultPatcher.TargetRelease.set' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(18,21): warning CA1416: This call site is reachable on all platforms. 'RunPreferences.ActionsForEmptyArgs.set' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(14,26): warning CA1416: This call site is reachable on all platforms. 'SynthesisPipeline.Instance' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(16,28): warning CA1416: This call site is reachable on all platforms. 'RunPreferences' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(18,43): warning CA1416: This call site is reachable on all platforms. 'RunDefaultPatcher' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(14,26): warning CA1416: This call site is reachable on all platforms. 'SynthesisPipeline.AddPatch<ISkyrimMod, ISkyrimModGetter>(SynthesisPipeline.PatcherFunction<ISkyrimMod, ISkyrimModGetter>, PatcherPreferences?)' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(14,26): warning CA1416: This call site is reachable on all platforms. 'SynthesisPipeline.Run(string[], RunPreferences?)' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\Program.cs(20,25): warning CA1416: This call site is reachable on all platforms. 'RunDefaultPatcher.IdentifyingModKey.set' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\WeaponAnalyer.cs(80,32): warning CA1416: This call site is reachable on all platforms. 'IPatcherState<ISkyrimMod, ISkyrimModGetter>.LoadOrder.get' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(79,32): warning CA1416: This call site is reachable on all platforms. 'IPatcherState<ISkyrimMod, ISkyrimModGetter>.LoadOrder.get' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]
C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\WeaponAnalyer.cs(51,31): warning CA1416: This call site is reachable on all platforms. 'IPatcherState<ISkyrimMod, ISkyrimModGetter>.LoadOrder.get' is only supported on: 'Windows' 7.0 and later. [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]";

        List<string> lines = new();
        foreach (var line in rawMessage.AsSpan().SplitLines())
        {
            if (sut.IsApplicable(line.Line))
            {
                lines.Add(line.Line.ToString());
            }
        }
        lines
            .Should().BeEquivalentTo(new string[]
            {
                @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]",
                @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(158,50): error CS0266: Cannot implicitly convert type 'Mutagen.Bethesda.Skyrim.LeveledItem' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'. An explicit conversion exists (are you missing a cast?) [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]"
            });
    }
}