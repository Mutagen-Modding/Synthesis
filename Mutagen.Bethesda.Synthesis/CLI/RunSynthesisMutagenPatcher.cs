using CommandLine;
using Mutagen.Bethesda.Strings;
using Synthesis.Bethesda;

namespace Mutagen.Bethesda.Synthesis.CLI;

[Verb("run-patcher", HelpText = "Run the patcher")]
public class RunSynthesisMutagenPatcher
{
    [Option('s', "SourcePath", Required = false, HelpText = "Optional path pointing to the previous patcher result to build onto.  File name must in ModKey format")]
    public string? SourcePath { get; set; }

    [Option('o', "OutputPath", Required = true, HelpText = "Path where the patcher should place its resulting file.  File name must in ModKey format")]
    public string OutputPath { get; set; } = string.Empty;

    [Option('g', "GameRelease", Required = true, HelpText = "GameRelease data folder is related to.")]
    public GameRelease GameRelease { get; set; }

    [Option('d', "DataFolderPath", Required = true, HelpText = "Path to the data folder.")]
    public string DataFolderPath { get; set; } = string.Empty;

    [Option('l', "LoadOrderFilePath", Required = false, HelpText = "Path to the load order file to use.  This is typically plugins.txt.  This should be the file that the game will use to read in its load order.")]
    public string LoadOrderFilePath { get; set; } = string.Empty;

    [Option('e', "ExtraDataFolder", Required = false, HelpText = "Path to the user data folder dedicated for a patcher")]
    public string? ExtraDataFolder { get; set; }

    [Option('i', "InternalDataFolder", Required = false, HelpText = "Path to the internal data folder dedicated for a patcher")]
    public string? InternalDataFolder { get; set; }

    [Option('r', "PersistencePath", Required = false, HelpText = "Path to the shared FormKey allocation state")]
    public string? PersistencePath { get; set; }

    [Option('p', "PatcherName", Required = false, HelpText = "Name of the patcher to be recorded in the shared FormKey allocation state")]
    public string? PatcherName { get; set; }

    [Option('f', "DefaultDataFolderPath", Required = false, HelpText = "Path to the data folder as the patcher source code defines it.")]
    public string? DefaultDataFolderPath { get; set; }
        
    [Option('k', "ModKey", Required = false, HelpText = "ModKey associated with the patch being generated")]
    public string? ModKey { get; set; }

    [Option("LoadOrderIncludesCreationClub", Required = false, HelpText = "Whether the load order path file includes CC mods already")]
    public bool LoadOrderIncludesCreationClub { get; set; } = true;

    [Option("TargetLanguage", Required = false, HelpText = "What language to view as the default language")]
    public Language TargetLanguage { get; set; } = Language.English;

    [Option("Localize", Required = false, HelpText = "Whether to use STRINGS files during export")]
    public bool Localize { get; set; } = false;

    [Option("UseUtf8ForEmbeddedStrings", Required = false, HelpText = "Use UTF8 encoding when reading/writing localizable mod strings that are embedded")]
    public bool UseUtf8ForEmbeddedStrings { get; set; }

    [Option("UseUtf8ForStringsFiles", Required = false, HelpText = "Use UTF8 encoding when reading/writing localizable mod strings that are in strings files")]
    public bool UseUtf8ForStringsFiles { get; set; }

    [Option("HeaderVersionOverride", Required = false, HelpText = "Whether to override the header version when making a new mod object")]
    public float? HeaderVersionOverride { get; set; }

    [Option("FormIDRangeMode", Required = false, HelpText = "Whether to override the header version when making a new mod object")]
    public FormIDRangeMode FormIDRangeMode { get; set; }

    public override string ToString()
    {
        return $"{nameof(RunSynthesisMutagenPatcher)} => \n"
               + $"  {nameof(SourcePath)} => {SourcePath} \n"
               + $"  {nameof(OutputPath)} => {OutputPath} \n"
               + $"  {nameof(GameRelease)} => {GameRelease} \n"
               + $"  {nameof(DataFolderPath)} => {DataFolderPath} \n"
               + $"  {nameof(DefaultDataFolderPath)} => {DefaultDataFolderPath} \n"
               + $"  {nameof(LoadOrderFilePath)} => {LoadOrderFilePath}\n"
               + $"  {nameof(ExtraDataFolder)} => {ExtraDataFolder}\n"
               + $"  {nameof(InternalDataFolder)} => {InternalDataFolder}\n"
               + $"  {nameof(PersistencePath)} => {PersistencePath}\n"
               + $"  {nameof(LoadOrderIncludesCreationClub)} => {LoadOrderIncludesCreationClub}\n"
               + $"  {nameof(PatcherName)} => {PatcherName}\n"
               + $"  {nameof(TargetLanguage)} => {TargetLanguage}\n"
               + $"  {nameof(Localize)} => {Localize}\n"
               + $"  {nameof(ModKey)} => {ModKey}\n"
               + $"  {nameof(HeaderVersionOverride)} => {HeaderVersionOverride}\n"
               + $"  {nameof(FormIDRangeMode)} => {FormIDRangeMode}\n"
               + $"  {nameof(UseUtf8ForStringsFiles)} => {UseUtf8ForStringsFiles}\n"
               + $"  {nameof(UseUtf8ForEmbeddedStrings)} => {UseUtf8ForEmbeddedStrings}";
    }
}