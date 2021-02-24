using Noggog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableStringSettingsVM : EnumerableSettingsVM
    {
        private IEnumerable<string> _defaultVal;

        public EnumerableStringSettingsVM(
            MemberName memberName,
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> get,
            Action<ObservableCollection<IBasicSettingsNodeVM>> add,
            IEnumerable<string> defaultVal)
            : base(memberName, get, add)
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

        public static EnumerableStringSettingsVM Factory(MemberName memberName, object? defaultVal)
        {
            Func<JsonElement, TryGet<IBasicSettingsNodeVM>> import = new Func<JsonElement, TryGet<IBasicSettingsNodeVM>>((elem) =>
            {
                return TryGet<IBasicSettingsNodeVM>.Succeed(
                    new ListElementWrapperVM<string, StringSettingsVM>(
                        new StringSettingsVM()
                        {
                            Value = elem.GetString() ?? string.Empty
                        }));
            });
            return new EnumerableStringSettingsVM(
                memberName,
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
            return new EnumerableStringSettingsVM(MemberName, _import, _add, _defaultVal);
        }
    }
}
