using Noggog.WPF;
using Noggog;
using ReactiveUI;
using System.Reactive.Disposables;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using DynamicData;

namespace Synthesis.Bethesda.GUI.Views
{
    public class PatchersConfigViewBase : NoggogUserControl<ConfigurationVM> { }

    /// <summary>
    /// Interaction logic for PatchersConfigurationView.xaml
    /// </summary>
    public partial class PatchersConfigView : PatchersConfigViewBase
    {
        private const string DragParamName = "dragData";

        public PatchersConfigView()
        {
            InitializeComponent();
            this.WhenActivated((disposable) =>
            {
                this.WhenAnyFallback(x => x.ViewModel.SelectedProfile!.AddGithubPatcherCommand, fallback: default(ICommand))
                    .BindToStrict(this, x => x.AddGithubButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel.SelectedProfile!.AddSolutionPatcherCommand, fallback: default(ICommand))
                    .BindToStrict(this, x => x.AddSolutionButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel.SelectedProfile!.AddCliPatcherCommand, fallback: default(ICommand))
                    .BindToStrict(this, x => x.AddCliButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyFallback(x => x.ViewModel.SelectedProfile!.AddSnippetPatcherCommand, fallback: default(ICommand))
                    .BindToStrict(this, x => x.AddSnippetButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.PatchersDisplay)
                    .BindToStrict(this, x => x.PatchersList.ItemsSource)
                    .DisposeWith(disposable);

                this.BindStrict(this.ViewModel, vm => vm.SelectedPatcher, view => view.PatchersList.SelectedItem)
                    .DisposeWith(disposable);

                // Wire up patcher config data context and visibility
                this.WhenAnyValue(x => x.ViewModel.DisplayedObject)
                    .BindToStrict(this, x => x.DetailControl.Content)
                    .DisposeWith(disposable);

                // Only show help if zero patchers
                this.WhenAnyValue(x => x.ViewModel.PatchersDisplay.Count)
                    .Select(c => c == 0 ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.AddSomePatchersHelpGrid.Visibility)
                    .DisposeWith(disposable);

                // Show dimmer if in initial configuration
                this.WhenAnyValue(x => x.ViewModel.NewPatcher)
                    .Select(newPatcher => newPatcher != null? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.InitialConfigurationDimmer.Visibility)
                    .DisposeWith(disposable);

                // Set up go button
                this.WhenAnyValue(x => x.ViewModel.RunPatchers)
                    .BindToStrict(this, x => x.GoButton.Command)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RunPatchers.CanExecute)
                    .Switch()
                    .Select(can => can ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.GoButton.Visibility)
                    .DisposeWith(disposable);
                this.WhenAnyValue(x => x.ViewModel.RunPatchers.CanExecute)
                    .Switch()
                    .CombineLatest(this.WhenAnyFallback(x => x.ViewModel.SelectedProfile!.LargeOverallError, ErrorResponse.Success),
                        (can, overall) => !can && overall.Succeeded)
                    .Select(show => show ? Visibility.Visible : Visibility.Collapsed)
                    .BindToStrict(this, x => x.ProcessingRingAnimation.Visibility)
                    .DisposeWith(disposable);

                // Set up large overall error icon display
                var overallErr = this.WhenAnyFallback( x => x.ViewModel.SelectedProfile!.LargeOverallError, fallback: ErrorResponse.Success)
                    .Replay(1)
                    .RefCount();
                overallErr.Select(x => x.Succeeded ? Visibility.Collapsed : Visibility.Visible)
                    .BindToStrict(this, x => x.OverallErrorIcon.Visibility)
                    .DisposeWith(disposable);
                overallErr.Select(x => x.Reason)
                    .BindToStrict(this, x => x.OverallErrorIcon.ToolTip)
                    .DisposeWith(disposable);

                // Set up drag and drop systems
                var startPt = Observable.Merge(
                        PatchersList.Events().PreviewMouseLeftButtonDown
                            .Select(e =>
                            {
                                var item = VisualTreeHelper.HitTest(this.PatchersList, Mouse.GetPosition(this.PatchersList)).VisualHit;
                                if (!item.TryGetAncestor<ListBoxItem>(out var hoveredItem))
                                {
                                    return (default(ListBoxItem?), default(Point?));
                                }
                                return (hoveredItem, e.GetPosition(PatchersList));
                            }),
                        PatchersList.Events().PreviewMouseLeftButtonUp
                            .Select(e => (default(ListBoxItem?), default(Point?))))
                    .DistinctUntilChanged()
                    .Replay(1)
                    .RefCount();

                PatchersList.Events().MouseMove
                    .FilterSwitch(startPt.Select(p => p.Item1 != null && p.Item2 != null))
                    .Where(x => x.LeftButton == MouseButtonState.Pressed)
                    .WithLatestFrom(
                        startPt,
                        (move, start) => (move, start))
                    .Subscribe(e =>
                    {
                        if (e.start.Item1 == null || e.start.Item2 == null) return;
                        var startPt = e.start.Item2.Value;
                        var position = e.move.GetPosition(PatchersList);
                        if (Math.Abs(position.X - startPt.X) > SystemParameters.MinimumHorizontalDragDistance 
                            || Math.Abs(position.Y - startPt.Y) > SystemParameters.MinimumVerticalDragDistance)
                        {
                            BeginDrag(e.move, e.start.Item1, startPt);
                        }
                    })
                    .DisposeWith(disposable);

                PatchersList.Events().Drop
                    .Subscribe(e =>
                    {
                        if (!e.Data.GetDataPresent(DragParamName)) return;
                        var vm = e.Data.GetData(DragParamName) as PatcherVM;
                        if (vm == null) return;
                        var profile = ViewModel.SelectedProfile;
                        if (!object.ReferenceEquals(profile, vm.Profile)) return;

                        if (!(e.OriginalSource is DependencyObject dep)) return;
                        if (!dep.TryGetAncestor<ListBoxItem>(out var targetItem)) return;
                        if (!(targetItem.DataContext is PatcherVM targetPatcher)) return;
                        var index = profile.Patchers.Items.IndexOf(targetPatcher);

                        if (index >= 0)
                        {
                            profile.Patchers.Remove(vm);
                            profile.Patchers.Insert(index, vm);
                        }
                        else
                        {
                            profile.Patchers.Remove(vm);
                            profile.Patchers.Add(vm);
                        }
                    })
                    .DisposeWith(disposable);
            });
        }

        #region Drag Drop
        private void BeginDrag(MouseEventArgs e, ListBoxItem listBoxItem, Point startPoint)
        {
            if (!(listBoxItem.DataContext is PatcherVM patcherVM)) return;

            var listBox = PatchersList;

            //setup the drag adorner.
            var adorner = InitialiseAdorner(listBoxItem);

            //add handles to update the adorner.
            DragEventHandler previewDrag = (object sender, DragEventArgs e) =>
            {
                adorner.OffsetLeft = e.GetPosition(listBox).X;
                adorner.OffsetTop = e.GetPosition(listBox).Y - startPoint.Y;
            };
            DragEventHandler enter = (object sender, DragEventArgs e) =>
            {
                if (!e.Data.GetDataPresent(DragParamName) || sender == e.Source)
                {
                    e.Effects = DragDropEffects.None;
                }
            };
            listBox.PreviewDragOver += previewDrag;
            //listBox.DragLeave += dragLeave;
            listBox.DragEnter += enter;

            DataObject data = new DataObject(DragParamName, patcherVM);
            DragDropEffects de = DragDrop.DoDragDrop(listBox, data, DragDropEffects.Move);

            //cleanup
            listBox.PreviewDragOver -= previewDrag;
            //listBox.DragLeave -= dragLeave;
            listBox.DragEnter -= enter;

            AdornerLayer.GetAdornerLayer(listBox).Remove(adorner);
        }

        private DragAdorner InitialiseAdorner(ListBoxItem listBoxItem)
        {
            VisualBrush brush = new VisualBrush(listBoxItem);
            var adorner = new DragAdorner((UIElement)listBoxItem, listBoxItem.RenderSize, brush);
            adorner.Opacity = 0.5;
            var layer = AdornerLayer.GetAdornerLayer(PatchersList as Visual);
            layer.Add(adorner);
            return adorner;
        }
        #endregion
    }
}
