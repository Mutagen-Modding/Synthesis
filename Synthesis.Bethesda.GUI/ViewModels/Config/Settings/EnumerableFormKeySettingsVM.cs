using Mutagen.Bethesda;
using Newtonsoft.Json.Linq;
using Noggog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Input;

namespace Synthesis.Bethesda.GUI
{
    public class EnumerableFormKeySettingsVM : EnumerableSettingsVM
    {
        private FormKey[] _defaultVal;

        public EnumerableFormKeySettingsVM(
            FieldMeta fieldMeta,
            IEnumerable<FormKey> defaultVal)
            : base(
                  fieldMeta,
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

        public static EnumerableFormKeySettingsVM Factory(FieldMeta fieldMeta, object? defaultVal)
        {
            return new EnumerableFormKeySettingsVM(
                fieldMeta,
                defaultVal as IEnumerable<FormKey> ?? Enumerable.Empty<FormKey>());
        }

        public override void Persist(JObject obj, Action<string> logger)
        {
            obj[Meta.DiskName] = new JArray(Values.Select(x => FormKeySettingsVM.Persist(((FormKeySettingsVM)x.Value).Value)).ToArray());
        }

        public override SettingsNodeVM Duplicate()
        {
            return new EnumerableFormKeySettingsVM(Meta, _defaultVal);
        }

        public override void WrapUp()
        {
            _defaultVal = _defaultVal.Select(x => FormKeySettingsVM.StripOrigin(x)).ToArray();
            Values.SetTo(_defaultVal.Select(x =>
            {
                return new ListElementWrapperVM<FormKey, FormKeySettingsVM>(new FormKeySettingsVM()
                {
                    Value = x
                });
            }));
        }
    }
}
