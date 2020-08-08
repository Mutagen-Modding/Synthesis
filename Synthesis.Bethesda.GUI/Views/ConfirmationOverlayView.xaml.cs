using Noggog.WPF;
using ReactiveUI;
using System;
using System.Reactive.Disposables;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Synthesis.Bethesda.GUI.Views
{
    public class ConfirmationOverlayViewBase : NoggogUserControl<MainVM> { }

    /// <summary>
    /// Interaction logic for ConfirmationOverlayView.xaml
    /// </summary>
    public partial class ConfirmationOverlayView : ConfirmationOverlayViewBase
    {
        public ConfirmationOverlayView()
        {
            InitializeComponent();
            this.WhenActivated(disposable =>
            {
                this.WhenAnyValue(x => x.ViewModel.ActiveConfirmation, x => x.ViewModel.ActiveConfirmation!.Title,
                        (c, _) => c?.Title)
                    .BindToStrict(this, x => x.TitleBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.ActiveConfirmation, x => x.ViewModel.ActiveConfirmation!.Description,
                        (c, _) => c?.Description)
                    .BindToStrict(this, x => x.DescriptionBlock.Text)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.ConfirmActionCommand)
                    .BindToStrict(this, x => x.AcceptButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.DiscardActionCommand)
                    .BindToStrict(this, x => x.CancelButton.Command)
                    .DisposeWith(disposable);
            });
        }
    }
}
