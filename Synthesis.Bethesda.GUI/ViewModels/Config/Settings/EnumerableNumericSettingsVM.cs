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
        public EnumerableNumericSettingsVM(
            string memberName,
            Func<JsonElement, IBasicSettingsNodeVM> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add)
            : base(memberName, get, add)
        {
        }

        public static EnumerableNumericSettingsVM Factory<TItem, TWrapper>(string memberName, object? defaultVal, TWrapper prototype)
            where TWrapper : BasicSettingsVM<TItem>, new()
        {
            EnumerableNumericSettingsVM ret = null!;
            Func<JsonElement, IBasicSettingsNodeVM> import = new Func<JsonElement, IBasicSettingsNodeVM>((elem) =>
            {
                return new ListElementWrapperVM<TItem, TWrapper>(
                    prototype.Get(elem));
            });
            ret = new EnumerableNumericSettingsVM(
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
    }
}
