using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumerableNumericSettingsVM : EnumerableSettingsVM
    {
        private readonly Action<ObservableCollection<IBasicSettingsNodeVM>, object?> _setToDefault;
        private readonly object? _defaultVal;

        public EnumerableNumericSettingsVM(
            FieldMeta fieldMeta,
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add,
            Action<ObservableCollection<IBasicSettingsNodeVM>, object?> setToDefault,
            object? defaultVal)
            : base(fieldMeta, get, add)
        {
            _setToDefault = setToDefault;
            _defaultVal = defaultVal;
        }

        public static EnumerableNumericSettingsVM Factory<TItem, TWrapper>(FieldMeta fieldMeta, object? defaultVal, TWrapper prototype)
            where TWrapper : BasicSettingsVM<TItem>, new()
        {
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> import = new((elem) =>
            {
                return TryGet<IBasicSettingsNodeVM>.Succeed(
                    new ListElementWrapperVM<TItem, TWrapper>(
                        new TWrapper()
                        {
                            Value = prototype.Get(elem)
                        }));
            });
            return new EnumerableNumericSettingsVM(
                fieldMeta,
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
            return new EnumerableNumericSettingsVM(Meta, _import, _add, _setToDefault, _defaultVal);
        }
    }
}
