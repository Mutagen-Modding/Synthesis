﻿using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Synthesis.Bethesda.GUI.Views;

/// <summary>
/// Interaction logic for ProfileListingView.xaml
/// </summary>
public partial class ProfileListingView
{
    public ProfileListingView()
    {
        InitializeComponent();
        this.WhenActivated(dispose =>
        {
            this.WhenAnyFallback(x => x.ViewModel!.Profile!.NameVm.Name, fallback: string.Empty)
                .Select(x => x)
                .BindTo(this, x => x.NameBlock.Text)
                .DisposeWith(dispose);

            this.WhenAnyValue(x => x.ViewModel!.SwitchToCommand)
                .BindTo(this, x => x.SelectButton.Command)
                .DisposeWith(dispose);
        });
    }
}