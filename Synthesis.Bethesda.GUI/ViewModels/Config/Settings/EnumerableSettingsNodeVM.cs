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
    public class EnumerableSettingsNodeVM : SettingsNodeVM
    {
        private Func<JsonElement, IBasicSettingsNodeVM> _get;
        public ObservableCollection<IBasicSettingsNodeVM> Values { get; } = new ObservableCollection<IBasicSettingsNodeVM>();
        public ICommand AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; }

        [Reactive]
        public IList? SelectedValues { get; set; }

        public EnumerableSettingsNodeVM(string memberName, Func<JsonElement, IBasicSettingsNodeVM> get)
            : base(memberName)
        {
            _get = get;
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
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                Values.Add(_get(elem));
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values.Select(x => ((IBasicSettingsNodeVM)x.Value).Value).ToArray());
        }

        public static EnumerableSettingsNodeVM Factory<TItem, TWrapper>(string memberName, object? defaultVal, TWrapper prototype)
            where TWrapper : BasicSettingsNodeVM<TItem>, new()
        {
            EnumerableSettingsNodeVM ret = null!;
            Func<JsonElement, IBasicSettingsNodeVM> add = new Func<JsonElement, IBasicSettingsNodeVM>((elem) =>
            {
                return new ListElementWrapperVM<TItem, TWrapper>(
                    prototype.Get(elem));
            });
            ret = new EnumerableSettingsNodeVM(
                memberName,
                add);
            if (defaultVal is IEnumerable<TItem> items)
            {
                ret.Values.SetTo(items.Select(x =>
                {
                    return new ListElementWrapperVM<TItem, TWrapper>(x);
                }));
            }
            ret.AddCommand = ReactiveCommand.Create(() => ret.Values.Add(new ListElementWrapperVM<TItem, TWrapper>(prototype.GetDefault())));
            return ret;
        }
    }
}
