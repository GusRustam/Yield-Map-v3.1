using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Reflection;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Reader {
    public class NEntityReaderHelper : INEntityReaderHelper {
        private readonly Dictionary<Type, string> _queries = new Dictionary<Type, string>();
        private readonly Dictionary<Type, PropertyRecord[]> _properties = new Dictionary<Type, PropertyRecord[]>();

        public NEntityReaderHelper() {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsClass && t.GetInterfaces().Contains(typeof(INotIdentifyable)))
                .ToList();

            foreach (var type in types) {
                PrepareProperties(type);

                var typeName = type.Name;
                var name = typeName.StartsWith("N") ? typeName.Substring(1) : typeName;

                var allFields = string.Join(", ", _properties[type].Select(p => p.DbName));

                _queries.Add(type, string.Format("SELECT {1} FROM {0}", name, allFields));
            }
        }

        private void PrepareProperties(Type type) {
            if (_properties.ContainsKey(type))
                return;

            _properties.Add(type,
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Select(p => new { Descr = p.GetCustomAttribute<DbFieldAttribute>(), Property = p })
                    .Where(x => x.Descr != null)
                    .OrderBy(x => x.Descr.Order)
                    .Select(x => new PropertyRecord(x.Property, x.Descr.Name))
                    .ToArray());
        }

        public string SelectSql<T>() {
            return _queries[typeof (T)];
        }

        public T Read<T>(SQLiteDataReader reader) where T : class, INotIdentifyable {
            // todo this can be also automated via Reflection.Emit
            if (reader.Read()) {
                if (typeof (T) == typeof (NInstrumentDescriptionView))
                    return (new NInstrumentDescriptionView {
                        id_Instrument = reader.GetInt32(0),
                        InstrumentName = reader.GetString(1),
                        InstrumentTypeName = reader.GetString(2),
                        id_InstrumentType = reader.GetInt32(3),
                        IssueSize= reader.GetNullableInt32(4),
                        Series= reader.GetString(5),
                        Issue = reader.GetNullableDateTime(6),
                        Maturity= reader.GetNullableDateTime(7),
                        NextCoupon= reader.GetNullableDateTime(8),
                        TickerName= reader.GetString(9),
                        SubIndustryName= reader.GetString(10),
                        IndustryName= reader.GetString(11),
                        SpecimenName= reader.GetString(12),
                        SeniorityName= reader.GetString(13),
                        RicName= reader.GetString(14),
                        IsinName= reader.GetString(15),
                        BorrowerName= reader.GetString(16),
                        BorrowerCountryName= reader.GetString(17),
                        IssuerName= reader.GetString(18),
                        IssuerCountryName= reader.GetString(19),
                        InstrumentRating= reader.GetString(20),
                        InstrumentRatingDate= reader.GetNullableDateTime(21),
                        InstrumentRatingAgency= reader.GetString(22),
                        IssuerRating= reader.GetString(23),
                        IssuerRatingDate= reader.GetNullableDateTime(24),
                        IssuerRatingAgency = reader.GetString(25)  
                    }) as T;
                if (typeof(T) == typeof(NBondDescriptionView))
                    return (new NBondDescriptionView {
                        id_Instrument = reader.GetInt32(0),
                        InstrumentName = reader.GetNullableString(1),
                        InstrumentTypeName = reader.GetNullableString(2),
                        id_InstrumentType = reader.GetInt32(3),
                        IssueSize = reader.GetNullableInt32(4),
                        Series = reader.GetNullableString(5),
                        Issue = reader.GetNullableDateTime(6),
                        Maturity = reader.GetNullableDateTime(7),
                        NextCoupon = reader.GetNullableDateTime(8),
                        TickerName = reader.GetNullableString(9),
                        SubIndustryName = reader.GetNullableString(10),
                        IndustryName = reader.GetNullableString(11),
                        SpecimenName = reader.GetNullableString(12),
                        SeniorityName = reader.GetNullableString(13),
                        RicName = reader.GetNullableString(14),
                        IsinName = reader.GetNullableString(15),
                        BorrowerName = reader.GetNullableString(16),
                        BorrowerCountryName = reader.GetNullableString(17),
                        IssuerName = reader.GetNullableString(18),
                        IssuerCountryName = reader.GetNullableString(19),
                        InstrumentRating = reader.GetNullableString(20),
                        InstrumentRatingDate = reader.GetNullableDateTime(21),
                        InstrumentRatingAgency = reader.GetNullableString(22),
                        IssuerRating = reader.GetNullableString(23),
                        IssuerRatingDate = reader.GetNullableDateTime(24),
                        IssuerRatingAgency = reader.GetNullableString(25),
                        BondStructure = reader.GetNullableString(26),
                        Coupon = reader.GetNullableDouble(27)
                    }) as T;

                if (typeof(T) == typeof(NOrdinaryBond))
                    return (new NOrdinaryBond {
                        id_Instrument = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Series = reader.GetString(2),
                        IssueSize = reader.GetNullableInt32(3),
                        RateStructure = reader.GetString(4),
                        BondStructure = reader.GetNullableString(5),
                        Isin = reader.GetNullableString(6),
                        Ric = reader.GetNullableString(7),
                        id_Isin = reader.GetInt32(8),
                        id_Ric = reader.GetInt32(9),
                        id_Ticker = reader.GetNullableInt32(10),
                        id_SubIndustry = reader.GetNullableInt32(11),
                        id_Specimen = reader.GetNullableInt32(12),
                        id_Seniority = reader.GetNullableInt32(13),
                        Issue = reader.GetNullableDateTime(14),
                        Maturity = reader.GetNullableDateTime(15),
                        NextCoupon = reader.GetNullableDateTime(16),
                        Coupon = reader.GetNullableDouble(17),
                        id_Currency = reader.GetNullableInt32(18)
                    }) as T;
                throw new ArgumentException("what");
            }
            return null;
        }
    }
}