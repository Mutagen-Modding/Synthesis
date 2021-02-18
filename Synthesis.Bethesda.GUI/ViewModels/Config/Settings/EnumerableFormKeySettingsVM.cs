using DynamicData;
using Mutagen.Bethesda;
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
    public class EnumerableFormKeySettingsVM : EnumerableSettingsVM
    {
        private readonly FormKey[] _defaultVal;

        public EnumerableFormKeySettingsVM(
            string memberName,
            IEnumerable<FormKey> defaultVal)
            : base(
                  memberName,
                  get: e => TryGet<IBasicSettingsNodeVM>.Succeed(
                      new ListElementWrapperVM<FormKey, FormKeySettingsVM>(new FormKeySettingsVM()
                      {
                          Value = FormKeySettingsVM.Import(e)
                      })),
                  add: coll => coll.Add(new ListElementWrapperVM<FormKey, FormKeySettingsVM>(new FormKeySettingsVM()
                  {
                      Value = FormKey.Null
                  })
                  {
                      IsSelected = true
                  }))
        {
            _defaultVal = defaultVal.ToArray();
        }

        public static EnumerableFormKeySettingsVM Factory(string memberName, object? defaultVal)
        {
            return new EnumerableFormKeySettingsVM(
                memberName,
                defaultVal as IEnumerable<FormKey> ?? Enumerable.Empty<FormKey>());
        }

        public override void Persist(JObject obj, ILogger logger)
        {
            obj[MemberName] = new JArray(Values.Select(x => FormKeySettingsVM.Persist(((FormKeySettingsVM)x.Value).Value)).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableFormKeySettingsVM(MemberName, _defaultVal);
        }
    }
}
