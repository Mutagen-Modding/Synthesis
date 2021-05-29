using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Xml.Linq;
using DynamicData.Kernel;
using Noggog;
using Noggog.WPF;
using ReactiveUI;

namespace Synthesis.Bethesda.GUI
{
    public class NugetConfigErrorVM : ViewModel
    {
        private readonly ObservableAsPropertyHelper<bool> _InError;
        public bool InError => _InError.Value;

        private readonly ObservableAsPropertyHelper<IErrorSolution?> _Error;
        public IErrorSolution? Error => _Error.Value;
        
        public FilePath NugetConfigPath { get; }
        
        private NotExists NotExistsError { get; }
        private Corrupt CorruptError { get; }
        private MissingNugetOrg MissingNugetOrgError { get; }

        public NugetConfigErrorVM()
        {
            NugetConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NuGet",
                "Nuget.Config");
            NotExistsError = new NotExists(this);
            CorruptError = new Corrupt(this);
            MissingNugetOrgError = new MissingNugetOrg(this);
            _Error = Noggog.ObservableExt.WatchFile(NugetConfigPath)
                .StartWith(Unit.Default)
                .Select(_ =>
                {
                    if (!NugetConfigPath.Exists)
                    {
                        return NotExistsError;
                    }

                    if (File.ReadAllLines(NugetConfigPath).All(x => x.IsNullOrWhitespace()))
                    {
                        return NotExistsError;
                    }

                    XDocument doc;
                    try
                    {
                        doc = XDocument.Load(NugetConfigPath);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("Nuget.Config corrupt", e);
                        return CorruptError;
                    }
                    
                    var config = doc.Element("configuration");
                    if (config == null)
                    {
                        return NotExistsError;
                    }

                    var sources = config.Element("packageSources");
                    if (sources != null 
                        && sources.Elements("add")
                        .Select(x => x.Attribute("value"))
                        .NotNull()
                        .Any(attr => attr.Value.Equals("https://api.nuget.org/v3/index.json")))
                    {
                        return default(IErrorSolution?);
                    }

                    return MissingNugetOrgError;
                })
                .RetryWithBackOff<IErrorSolution?, Exception>((_, times) => TimeSpan.FromMilliseconds(Math.Min(times * 250, 5000)))
                .ToGuiProperty(this, nameof(Error), default);
            _InError = this.WhenAnyValue(x => x.Error)
                .Select(x => x != null)
                .ToGuiProperty(this, nameof(InError));
        }

        public interface IErrorSolution
        {
            string ErrorText { get; }
            ICommand? RunFix { get; }
        }

        class NotExists : IErrorSolution
        {
            public string ErrorText { get; protected set; }
            public ICommand? RunFix { get; }

            public NotExists(NugetConfigErrorVM parent)
            {
                ErrorText = $"Config did not exist or was empty.";
                RunFix = ReactiveCommand.Create(() =>
                {
                    try
                    {
                        var elem = new XElement("configuration",
                            new XElement("packageSources",
                                new XElement("add",
                                    new XAttribute("key", "nuget.org"),
                                    new XAttribute("value", "https://api.nuget.org/v3/index.json"),
                                    new XAttribute("protocolVersion", "3"))));
                        var doc = new XDocument(
                            new XDeclaration("1.0", "utf-8", null),
                            elem);
                        doc.Save(parent.NugetConfigPath);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("Could not apply Nuget Config fix", e);
                    }
                });
            }
        }

        class Corrupt : NotExists, IErrorSolution
        {
            public Corrupt(NugetConfigErrorVM parent) 
                : base(parent)
            {
                ErrorText = $"Config was corrupt.  Can fix by replacing the whole file.";
            }
        }

        class MissingNugetOrg : IErrorSolution
        {
            public string ErrorText { get; }
            public ICommand? RunFix { get; }

            public MissingNugetOrg(NugetConfigErrorVM parent)
            {
                ErrorText = "Config did not list nuget.org as a source";
                RunFix = ReactiveCommand.Create(() =>
                {
                    try
                    {
                        var doc = XElement.Load(parent.NugetConfigPath);
                        var sources = doc.Element("packageSources");
                        if (sources == null)
                        {
                            throw new DataException("Could not find package sources");
                        }

                        if (sources.Elements("add")
                            .Select(x => x.Attribute("value"))
                            .NotNull()
                            .Any(attr => attr.Value.Equals("https://api.nuget.org/v3/index.json")))
                        {
                            return;
                        }

                        sources.Add(
                            new XElement("add",
                                new XAttribute("key", "nuget.org"),
                                new XAttribute("value", "https://api.nuget.org/v3/index.json"),
                                new XAttribute("protocolVersion", "3")));
                        
                        doc.Save(parent.NugetConfigPath);
                    }
                    catch (Exception e)
                    {
                        Log.Logger.Error("Could not apply Nuget Config fix", e);
                    }
                });
            }
        }
    }
}