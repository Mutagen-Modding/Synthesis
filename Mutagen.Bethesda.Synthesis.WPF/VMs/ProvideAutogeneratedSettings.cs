﻿using System.Reactive.Linq;
using DynamicData;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Order;
using ReactiveUI;
using Synthesis.Bethesda;
using Synthesis.Bethesda.Execution.Patchers.Git;
using Synthesis.Bethesda.Execution.Patchers.Git.Services;

namespace Mutagen.Bethesda.Synthesis.WPF;

public interface IProvideAutogeneratedSettings
{
    AutogeneratedSettingsVm Get(
        SettingsConfiguration config, 
        TargetProject targetProject,
        IObservable<IChangeSet<IModListingGetter>> loadOrder, 
        IObservable<ILinkCache?> linkCache);
}

public class ProvideAutogeneratedSettings : IProvideAutogeneratedSettings
{
    private readonly IProvideReflectionSettingsBundle _provideBundle;

    public ProvideAutogeneratedSettings(
        IProvideReflectionSettingsBundle provideBundle)
    {
        _provideBundle = provideBundle;
    }
        
    public AutogeneratedSettingsVm Get(
        SettingsConfiguration config, 
        TargetProject targetProject,
        IObservable<IChangeSet<IModListingGetter>> loadOrder, 
        IObservable<ILinkCache?> linkCache)
    {
        return new AutogeneratedSettingsVm(
            config, targetProject, loadOrder.ObserveOn(RxApp.MainThreadScheduler), linkCache,
            _provideBundle);
    }
}