using System;
using YieldMap.Database;
using YieldMap.Transitive.Enums;

namespace YieldMap.Transitive.Tools {
    public class FieldResolver : IFieldResolver {
        private readonly IFieldGroups _groups;

        public FieldResolver(IFieldGroups groups) {
            _groups = groups;
        }

        public FieldGroup Resolve(String ric) {
            return ric.Contains("=MM") ? _groups.Micex : _groups.Default;
        }
    }
}