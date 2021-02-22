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
using System.Reflection;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableObjectSettingsVM : SettingsNodeVM
    {
        public class SelectionWrapper
        {
            [Reactive]
            public bool IsSelected { get; set; }

            public ObjectSettingsVM Value { get; set; } = null!;
        }

        private ObjectSettingsVM _prototype;

        public ObservableCollection<SelectionWrapper> Values { get; } = new ObservableCollection<SelectionWrapper>();
        public ReactiveCommand<Unit, Unit> AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; } = null!;

        [Reactive]
        public IList? SelectedValues { get; set; }

        private EnumerableObjectSettingsVM(string memberName, ObjectSettingsVM prototype)
            : base(memberName)
        {
            _prototype = prototype;
            Init();
        }

        public EnumerableObjectSettingsVM(SettingsParameters param, string memberName, Type t)
            : base(memberName)
        {
            _prototype = new ObjectSettingsVM(param, string.Empty, t, null);
            Init();
        }

        private void Init()
        {
            DeleteCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    if (SelectedValues == null) return;
                    SelectionWrapper[] vms = new SelectionWrapper[SelectedValues.Count];
                    for (int i = 0; i < vms.Length; i++)
                    {
                        vms[i] = (SelectionWrapper)SelectedValues[i]!;
                    }
                    Values.Remove(vms);
                },
                canExecute: this.WhenAnyValue(x => x.SelectedValues!.Count)
                    .Select(x => x > 0));
            AddCommand = ReactiveCommand.Create(
                execute: () =>
                {
                    var vm = (ObjectSettingsVM)_prototype.Duplicate();
                    vm.WrapUp();
                    Values.Add(new SelectionWrapper()
                    {
                        IsSelected = true,
                        Value = vm
                    });
                });
        }

        public override void Import(JsonElement property, ILogger logger)
        {
            Values.Clear();
            foreach (var elem in property.EnumerateArray())
            {
                var dup = (ObjectSettingsVM)_prototype.Duplicate();
                dup.Import(elem, logger);
                dup.WrapUp();
                Values.Add(new SelectionWrapper()
                {
                    Value = dup
                });
            }
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values
                .Select(x =>
                {
                    var obj = new JObject();
                    x.Value.Persist(obj, logger);
                    return obj;
                })
                .ToArray());
        }

        public static EnumerableObjectSettingsVM Factory<TItem, TWrapper>(SettingsParameters param, string memberName, object? defaultVal, Type t)
            where TWrapper : BasicSettingsVM<TItem>, new()
        {
            var ret = new EnumerableObjectSettingsVM(param, memberName, t);
            if (defaultVal != null)
            {
                throw new NotImplementedException();
            }
            return ret;
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableObjectSettingsVM(MemberName, this._prototype);
        }
    }
}
