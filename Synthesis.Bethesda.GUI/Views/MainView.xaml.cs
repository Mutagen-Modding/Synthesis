using Noggog.WPF;
using ReactiveUI;
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
        }
    }
}
