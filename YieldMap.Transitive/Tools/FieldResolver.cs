using System;
using YieldMap.Transitive.Enums;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Tools {
    public class FieldResolver : IFieldResolver {
        private readonly IFieldGroups _groups;

        public FieldResolver(IFieldGroups groups) {
            _groups = groups;
        }

        public NFieldGroup Resolve(String ric) {
            return ric.Contains("=MM") ? _groups.Micex : _groups.Default;
        }
    }
}