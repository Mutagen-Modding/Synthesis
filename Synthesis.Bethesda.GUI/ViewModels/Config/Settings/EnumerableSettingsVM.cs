using Newtonsoft.Json.Linq;
using Noggog;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableSettingsVM : SettingsNodeVM
    {
        private Func<JsonElement, IBasicSettingsNodeVM> _import;
        private Action<ObservableCollection<IBasicSettingsNodeVM>> _add;
        public ObservableCollection<IBasicSettingsNodeVM> Values { get; } = new ObservableCollection<IBasicSettingsNodeVM>();
        public ReactiveCommand<Unit, Unit> AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; }

        [Reactive]
        public IList? SelectedValues { get; set; }

        public EnumerableSettingsVM(
            string memberName,
            Func<JsonElement, IBasicSettingsNodeVM> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add)
            : base(memberName)
        {
            _import = get;
            _add = add;
            DeleteCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    if (SelectedValues == null) return;
                    IBasicSettingsNodeVM[] vms = new IBasicSettingsNodeVM[SelectedValues.Count];
                    for (int i = 0; i < vms.Length; i++)
                    {
                        vms[i] = (IBasicSettingsNodeVM)SelectedValues[i]!;
                    }
                    Values.Remove(vms);
                },
                canExecute: this.WhenAnyValue(x => x.SelectedValues!.Count)
                    .Select(x => x > 0));
            AddCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    _add(Values);
                });
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                Values.Add(_import(elem));
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values.Select(x => ((IBasicSettingsNodeVM)x.Value).Value).ToArray());
        }

        public static EnumerableSettingsVM Factory<TItem, TWrapper>(string memberName, object? defaultVal, TWrapper prototype)
            where TWrapper : BasicSettingsVM<TItem>, new()
        {
            EnumerableSettingsVM ret = null!;
            Func<JsonElement, IBasicSettingsNodeVM> import = new Func<JsonElement, IBasicSettingsNodeVM>((elem) =>
            {
                return new ListElementWrapperVM<TItem, TWrapper>(
                    prototype.Get(elem));
            });
            ret = new EnumerableSettingsVM(
                memberName,
                import,
                (list) =>
                {
                    list.Add(new ListElementWrapperVM<TItem, TWrapper>(prototype.GetDefault())
                    {
                        IsSelected = true
                    });
                });
            if (defaultVal is IEnumerable<TItem> items)
            {
                ret.Values.SetTo(items.Select(x =>
                {
                    return new ListElementWrapperVM<TItem, TWrapper>(x);
                }));
            }
            return ret;
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableSettingsVM(string.Empty, _import, _add);
        }
    }
}
