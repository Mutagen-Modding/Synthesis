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
    public class EnumerableNumericSettingsVM : EnumerableSettingsVM
    {
        private Action<ObservableCollection<IBasicSettingsNodeVM>, object?> _setToDefault;
        private object? _defaultVal;

        public EnumerableNumericSettingsVM(
            string memberName,
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add,
            Action<ObservableCollection<IBasicSettingsNodeVM>, object?> setToDefault,
            object? defaultVal)
            : base(memberName, get, add)
        {
            _setToDefault = setToDefault;
            _defaultVal = defaultVal;
        }

        public static EnumerableNumericSettingsVM Factory<TItem, TWrapper>(string memberName, object? defaultVal, TWrapper prototype)
            where TWrapper : BasicSettingsVM<TItem>, new()
        {
            EnumerableNumericSettingsVM ret = null!;
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> import = new Func<JsonElement, TryGet<IBasicSettingsNodeVM>>((elem) =>
            {
                return TryGet<IBasicSettingsNodeVM>.Succeed(
                    new ListElementWrapperVM<TItem, TWrapper>(
                        new TWrapper()
                        {
                            Value = prototype.Get(elem)
                        }));
            });
            return new EnumerableNumericSettingsVM(
                memberName,
                import,
                (list) =>
                {
                    list.Add(new ListElementWrapperVM<TItem, TWrapper>(new TWrapper()
                    {
                        Value = prototype.GetDefault()
                    })
                    {
                        IsSelected = true
                    });
                },
                (list, def) =>
                {
                    if (def is IEnumerable<TItem> items)
                    {
                        list.SetTo(items.Select(x =>
                        {
                            return new ListElementWrapperVM<TItem, TWrapper>(new TWrapper()
                            {
                                Value = x
                            });
                        }));
                    }
                },
                defaultVal);
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableNumericSettingsVM(MemberName, _import, _add, _setToDefault, _defaultVal);
        }
    }
}
