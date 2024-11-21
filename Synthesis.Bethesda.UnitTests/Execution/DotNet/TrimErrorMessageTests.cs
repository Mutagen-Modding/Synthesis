using Synthesis.Bethesda.Execution.DotNet;
using Synthesis.Bethesda.UnitTests.AutoData;

namespace Synthesis.Bethesda.UnitTests.Execution.DotNet;

public class TrimErrorMessageTests
{
    [Theory, SynthAutoData]
    public void Typical(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]";
        var relativePath = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\";
        var expectedResult = @"HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }

    [Theory, SynthAutoData]
    public void MismatchedPath(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\Users\Levia\AppDataz\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]";
        var relativePath = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\";
        var expectedResult = @"C:\Users\Levia\AppDataz\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }

    [Theory, SynthAutoData]
    public void NoEndProj(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' ";
        var relativePath = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\";
        var expectedResult = @"HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }

    [Theory, SynthAutoData]
    public void EndsWithDelimiter(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [";
        var relativePath = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\";
        var expectedResult = @"HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly convert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }

    [Theory, SynthAutoData]
    public void HasDelimiter(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly c[onvert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>' [C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\HalgarisRPGLoot\HalgarisRPGLoot.csproj]";
        var relativePath = @"C:\Users\Levia\AppData\Local\Temp\jt3nyvxx.xjm\halgari\HalgarisRPGLoot\";
        var expectedResult = @"HalgarisRPGLoot\ArmorAnalyzer.cs(151,49): error CS0029: Cannot implicitly c[onvert type 'Mutagen.Bethesda.FormKey' to 'Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IItemGetter>'";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }

    [Theory, SynthAutoData]
    public void OddTrim(TrimErrorMessage sut)
    {
        var rawMessage = @"C:\\Users\\Levia\\AppData\\Local\\Temp\\tqhereth.tgq\\3ndos\\AddNewRecipes\\AddNewRecipes\\Program.cs(702,82): error CS0266: Cannot implicitly convert type 'System.Collections.Generic.IReadOnlyList<Mutagen.Bethesda.IFormLinkGetter<Mutagen.Bethesda.Skyrim.IKeywordGetter>>' to 'System.Collections.Generic.IReadOnlyList<Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IKeywordGetter>>'. An explicit conversion exists (are you missing a cast?) [C:\\Users\\Levia\\AppData\\Local\\Temp\\tqhereth.tgq\\3ndos\\AddNewRecipes\\AddNewRecipes\\AddNewRecipes.csproj]";
        var relativePath = @"C:\\Users\\Levia\\AppData\\Local\\Temp\\tqhereth.tgq\\3ndos\\AddNewRecipes\\3ndos\\";
        var expectedResult = @"C:\\Users\\Levia\\AppData\\Local\\Temp\\tqhereth.tgq\\3ndos\\AddNewRecipes\\AddNewRecipes\\Program.cs(702,82): error CS0266: Cannot implicitly convert type 'System.Collections.Generic.IReadOnlyList<Mutagen.Bethesda.IFormLinkGetter<Mutagen.Bethesda.Skyrim.IKeywordGetter>>' to 'System.Collections.Generic.IReadOnlyList<Mutagen.Bethesda.IFormLink<Mutagen.Bethesda.Skyrim.IKeywordGetter>>'. An explicit conversion exists (are you missing a cast?) [C:\\Users\\Levia\\AppData\\Local\\Temp\\tqhereth.tgq\\3ndos\\AddNewRecipes\\AddNewRecipes\\AddNewRecipes.csproj]";
        Assert.Equal(expectedResult, sut.Trim(rawMessage, relativePath).ToString());
    }
}