using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Noggog.WPF;
using System.Windows;
using System.Windows.Controls;
using DynamicData;
using Noggog.WPF.Containers;
using ReactiveUI;
using Noggog;
using Synthesis.Bethesda.GUI.ViewModels.Groups;
using Synthesis.Bethesda.GUI.ViewModels.Patchers.TopLevel;
using Synthesis.Bethesda.GUI.ViewModels.Top;

namespace Synthesis.Bethesda.GUI.Views;

public class MainConfigViewBase : NoggogUserControl<ProfileManagerVm> { }

/// <summary>
/// Interaction logic for PatchersConfigurationView.xaml
/// </summary>
public partial class MainConfigView : MainConfigViewBase
{
    public MainConfigView()
    {
        InitializeComponent();
        this.WhenActivated((disposable) =>
        {
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.GroupsDisplay)
                .BindTo(this, x => x.GroupsList.ItemsSource)
                .DisposeWith(disposable);

            this.Bind(this.ViewModel, vm => vm.SelectedProfile!.DisplayController.SelectedObject, view => view.GroupsList.SelectedValue)
                .DisposeWith(disposable);

            // Wire up patcher config data context and visibility
            this.WhenAnyValue(x => x.ViewModel!.DisplayedObject)
                .BindTo(this, x => x.DetailControl.Content)
                .DisposeWith(disposable);

            // Only show help if zero groups
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.GroupsDisplay.Count)
                .Select(c => c == 0 ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.AddSomePatchersHelpGrid.Visibility)
                .DisposeWith(disposable);

            // Show dimmer if in initial configuration
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.Init.NewPatcher)
                .Select(newPatcher => newPatcher != null ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.InitialConfigurationDimmer.Visibility)
                .DisposeWith(disposable);


            // Set up large overall error button
            var overallErr = this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.BlockingError, fallback: GetResponse<ViewModel>.Succeed(null!))
                .Replay(1)
                .RefCount();
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.GroupsDisplay.Count)
                        .Select(c => c > 0),
                    overallErr.Select(x => x.Succeeded),
                    (hasGroups, succeeded) => hasGroups && !succeeded)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.OverallErrorButton.Visibility)
                .DisposeWith(disposable);
            overallErr.Select(x => x.Reason)
                .BindTo(this, x => x.OverallErrorButton.ToolTip)
                .DisposeWith(disposable);
            this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.GoToErrorCommand)
                .BindTo(this, x => x.OverallErrorButton.Command)
                .DisposeWith(disposable);

            // Set up go button
            this.WhenAnyValue(x => x.ViewModel!.RunPatchers)
                .BindTo(this, x => x.GoButton.Command)
                .DisposeWith(disposable);
            overallErr
                .Select(err => err.Succeeded ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.GoButton.Visibility)
                .DisposeWith(disposable);

            this.WhenAnyValue(x => x.ViewModel!.RunPatchers.CanExecute)
                .Switch()
                .CombineLatest(this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.BlockingError, GetResponse<ViewModel>.Succeed(null!)),
                    (can, overall) => !can && overall.Succeeded)
                .Select(show => show ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.ProcessingCircle.Visibility)
                .DisposeWith(disposable);
                
            Drag.ListBoxDrops<ViewModel>(
                    this.GroupsList,
                    onlyWithinSameBox: false)
                .Subscribe(e =>
                {
                    if (e.Vm == null) return;
                    if (e.RawArgs.OriginalSource is not DependencyObject dep) return;
                    if (!dep.TryGetAncestor<ListBoxItem>(out var targetItem)) return;
                    if (targetItem.DataContext is not GroupVm targetGroup) return;
                    if (!targetItem.TryGetAncestor<ListBox>(out var targetListBox)) return;
                
                    if (e.SourceListBox == null) return;
                        
                    if (targetListBox.ItemsSource is not ISourceListUiFunnel<GroupVm> targetGroupList) return;

                    if (e.SourceListBox.ItemsSource is ISourceListUiFunnel<GroupVm> groupList)
                    {
                        if (e.Vm is not GroupVm groupVm) return;
                        var index = targetGroupList.IndexOf(targetGroup);
                        if (index >= targetGroupList.SourceList.Count) return;
                            
                        groupList.SourceList.Remove(groupVm);
                            
                        if (index >= 0)
                        {
                            targetGroupList.SourceList.Insert(index, groupVm);
                        }
                        else
                        {
                            targetGroupList.SourceList.Add(groupVm);
                        }
                    }
                    else if (e.SourceListBox.ItemsSource is ISourceListUiFunnel<PatcherVm> patcherList)
                    {
                        if (!targetItem.TryGetChildOfType<ListBox>(out var patcherListBox)) return;
                        if (patcherListBox.ItemsSource is not ISourceListUiFunnel<PatcherVm> targetPatcherList) return;
                        if (e.Vm is not PatcherVm patcherVm) return;
                            
                        patcherList.SourceList.Remove(patcherVm);
                            
                        targetPatcherList.SourceList.Add(patcherVm);
                    }
                })
                .DisposeWith(disposable);

            // Bind top patcher list buttons
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.GroupsDisplay.Count)
                .Select(c => c == 0 ? Visibility.Hidden : Visibility.Visible)
                .BindTo(this, x => x.TopAllPatchersControls.Visibility)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.EnableAllGroupsCommand)
                .BindTo(this, x => x.EnableAllGroupsButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.DisableAllGroupsCommand)
                .BindTo(this, x => x.DisableAllGroupsButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.ExpandAllGroupsCommand)
                .BindTo(this, x => x.ExpandAllGroupsButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.CollapseAllGroupsCommand)
                .BindTo(this, x => x.CollapseAllGroupsButton.Command)
                .DisposeWith(disposable);
            this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                .BindTo(this, x => x.UpdateAllPatchersButton.Command)
                .DisposeWith(disposable);
            Observable.CombineLatest(
                    this.WhenAnyValue(x => x.ViewModel!.SelectedProfile!.UpdateAllPatchersCommand)
                        .Select(x => x.CanExecute)
                        .Switch(),
                    this.WhenAnyFallback(x => x.ViewModel!.SelectedProfile!.LockSetting.Lock),
                    (hasUpdate, locked) => hasUpdate && !locked)
                .Select(x => x ? Visibility.Visible : Visibility.Collapsed)
                .BindTo(this, x => x.UpdateAllPatchersButton.Visibility)
                .DisposeWith(disposable);
        });
    }
}