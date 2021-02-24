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
        internal ObjectSettingsVM[] _defaultValues = Array.Empty<ObjectSettingsVM>();

        public ObservableCollection<SelectionWrapper> Values { get; } = new ObservableCollection<SelectionWrapper>();
        public ReactiveCommand<Unit, Unit> AddCommand { get; private set; } = null!;
        public ReactiveCommand<Unit, Unit> DeleteCommand { get; private set; } = null!;

        [Reactive]
        public IList? SelectedValues { get; set; }

        private EnumerableObjectSettingsVM(MemberName memberName, ObjectSettingsVM prototype, ObjectSettingsVM[] defaultValues)
            : base(memberName)
        {
            _prototype = prototype;
            _defaultValues = defaultValues;
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
            Values.SetTo(defaultValues.Select(o => new SelectionWrapper()
            {
                Value = (ObjectSettingsVM)o.Duplicate()
            }));
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
            obj[MemberName.DiskName] = new JArray(Values
                .Select(x =>
                {
                    var obj = new JObject();
                    x.Value.Persist(obj, logger);
                    return obj;
                })
                .ToArray());
        }

        public static EnumerableObjectSettingsVM Factory(SettingsParameters param, MemberName memberName, object? defaultVal, Type t)
        {
            var proto = new ObjectSettingsVM(param, MemberName.Empty, t, null);
            List<ObjectSettingsVM> defaultValues = new List<ObjectSettingsVM>();
            if (defaultVal is IEnumerable e)
            {
                foreach (var o in e)
                {
                    defaultValues.Add(new ObjectSettingsVM(param, MemberName.Empty, t, o));
                }
            }
            return new EnumerableObjectSettingsVM(memberName, proto, defaultValues.ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableObjectSettingsVM(MemberName, _prototype, _defaultValues);
        }
    }
}
