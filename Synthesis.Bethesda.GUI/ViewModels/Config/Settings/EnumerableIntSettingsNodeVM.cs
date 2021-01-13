using Newtonsoft.Json.Linq;
using Noggog;
using ReactiveUI;
using Serilog;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableIntSettingsNodeVM : SettingsNodeVM
    {
        public ObservableCollection<PrimitiveWrapperVM<int>> Values { get; } = new ObservableCollection<PrimitiveWrapperVM<int>>();
        public ICommand AddCommand { get; }

        public EnumerableIntSettingsNodeVM(string memberName, object? defaultVal)
            : base(memberName, typeof(IEnumerable<int>))
        {
            if (defaultVal is IEnumerable<int> ints)
            {
                Values.SetTo(ints.Select(x =>
                {
                    return new PrimitiveWrapperVM<int>(Remove)
                    {
                        Value = x
                    };
                }));
            }
            AddCommand = ReactiveCommand.Create(() =>
            {
                Values.Add(new PrimitiveWrapperVM<int>(Remove)
                {
                    Value = 0
                });
            });
        }

        public override void Import(JsonProperty property, ILogger logger)
        {
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values.Select(x => x.Value).ToArray());
        }

        private void Remove(PrimitiveWrapperVM<int> vm)
        {
            Values.Remove(vm);
        }
    }
}
