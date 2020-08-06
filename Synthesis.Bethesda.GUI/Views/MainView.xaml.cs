using Noggog.WPF;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;

namespace Synthesis.Bethesda.GUI.Views
{
    public class MainViewBase : NoggogUserControl<MainVM> { }

    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : MainViewBase
    {
        public MainView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.Configuration)
                    .BindToStrict(this, x => x.ConfigView.DataContext)
                    .DisposeWith(disposable);
            });
        }
    }
}
