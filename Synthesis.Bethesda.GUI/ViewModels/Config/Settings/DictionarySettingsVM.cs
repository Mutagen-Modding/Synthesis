using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class DictionarySettingsVM : ADictionarySettingsVM
    {
        public ICommand AddCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ConfirmCommand { get; }

        [Reactive]
        public string AddPaneText { get; set; } = string.Empty;

        [Reactive]
        public bool MidDelete { get; set; }

        private Dictionary<string, DictionarySettingItemVM> _dictionary;

        public DictionarySettingsVM(SettingsMeta memberName, KeyValuePair<string, SettingsNodeVM>[] values, SettingsNodeVM prototype)
            : base(memberName, values, prototype)
        {
            _dictionary = this.Items.ToDictionary(x => x.Key, x => x);
            AddCommand = ReactiveCommand.Create(
                canExecute: this.WhenAnyValue(x => x.AddPaneText)
                    .Select(x => !x.IsNullOrWhitespace() && !this._dictionary.ContainsKey(x)),
                execute: () =>
                {
                    var vm = prototype.Duplicate();
                    vm.WrapUp();
                    var item = new DictionarySettingItemVM(AddPaneText, vm);
                    _dictionary[AddPaneText] = item;
                    Items.Add(item);
                    AddPaneText = string.Empty;
                });
            DeleteCommand = ReactiveCommand.Create(
                canExecute: Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Selected).Select(x => x != null),
                    this.WhenAnyValue(x => x.MidDelete),
                    (hasSelected, midDelete) => hasSelected || midDelete),
                execute: () =>
                {
                    MidDelete = !MidDelete;
                });
            ConfirmCommand = ReactiveCommand.Create(
                canExecute: Observable.CombineLatest(
                    this.WhenAnyValue(x => x.Selected).Select(x => x != null),
                    this.WhenAnyValue(x => x.MidDelete),
                    (hasSelected, midDelete) => hasSelected && midDelete),
                execute: () =>
                {
                    if (Selected == null) return;
                    _dictionary.Remove(Selected.Key);
                    Items.Remove(Selected);
                    MidDelete = false;
                });
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            base.Import(property, logger);
            _dictionary = this.Items.ToDictionary(x => x.Key, x => x);
        }

        public static DictionarySettingsVM Factory(SettingsParameters param, SettingsMeta memberName, Type valType, object? defaultVal)
        {
            var vals = GetDefaultValDictionary(defaultVal);
            var proto = SettingsNodeVM.MemberFactory(param, member: null, targetType: valType, defaultVal: null);
            proto.WrapUp();
            return new DictionarySettingsVM(
                memberName,
                vals.Select(defVal =>
                {
                    return new KeyValuePair<string, SettingsNodeVM>(
                        defVal.Key.ToString()!,
                        SettingsNodeVM.MemberFactory(param, member: null, targetType: valType, defaultVal: defVal.Value));
                }).ToArray(),
                prototype: proto);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new DictionarySettingsVM(Meta, _values, _prototype);
        }
    }
}
