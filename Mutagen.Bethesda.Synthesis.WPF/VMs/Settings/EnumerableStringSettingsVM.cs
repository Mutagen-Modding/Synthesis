using Noggog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Mutagen.Bethesda.Synthesis.WPF
{
    public class EnumerableStringSettingsVM : EnumerableSettingsVM
    {
        private readonly IEnumerable<string> _defaultVal;

        public EnumerableStringSettingsVM(
            FieldMeta fieldMeta,
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add,
            IEnumerable<string> defaultVal)
            : base(fieldMeta, get, add)
        {
            _defaultVal = defaultVal;
            Values.SetTo(_defaultVal.Select(x =>
            {
                return new ListElementWrapperVM<string, StringSettingsVM>(new StringSettingsVM()
                {
                    Value = x
                });
            }));
        }

        public static EnumerableStringSettingsVM Factory(FieldMeta fieldMeta, object? defaultVal)
        {
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> import = new((elem) =>
            {
                return TryGet<IBasicSettingsNodeVM>.Succeed(
                    new ListElementWrapperVM<string, StringSettingsVM>(
                        new StringSettingsVM()
                        {
                            Value = elem.GetString() ?? string.Empty
                        }));
            });
            return new EnumerableStringSettingsVM(
                fieldMeta,
                import,
                (list) =>
                {
                    list.Add(new ListElementWrapperVM<string, StringSettingsVM>(new StringSettingsVM()
                    {
                        Value = string.Empty
                    })
                    {
                        IsSelected = true
                    });
                },
                defaultVal as IEnumerable<string> ?? Enumerable.Empty<string>());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableStringSettingsVM(Meta, _import, _add, _defaultVal);
        }
    }
}
