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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public abstract class EnumerableSettingsVM : SettingsNodeVM
    {
        protected Func<JsonElement, TryGet<IBasicSettingsNodeVM>> _import;
        protected Action<ObservableCollection<IBasicSettingsNodeVM>> _add;
        public ObservableCollection<IBasicSettingsNodeVM> Values { get; } = new ObservableCollection<IBasicSettingsNodeVM>();
        public ReactiveCommand<Unit, Unit> AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; }

        [Reactive]
        public IList? SelectedValues { get; set; }

        public EnumerableSettingsVM(
            FieldMeta fieldMeta,
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add)
            : base(fieldMeta)
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
                var item = _import(elem);
                if (item.Failed) continue;
                Values.Add(item.Value);
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[Meta.DiskName] = new JArray(Values.Select(x => ((IBasicSettingsNodeVM)x.Value).Value).ToArray());
        }
    }
}
