using System;
using System.Collections.Generic;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Linq;
using YieldMap.Database.Access;
using YieldMap.Requests.MetaTables;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class InstrumentFrns : AccessToDb {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Additions.InstrumentFrns");

        public IEnumerable<Tuple<MetaTables.FrnData, Exception>> SaveFrns(IEnumerable<MetaTables.FrnData> notes, bool useEf = false) {
            var res = new List<Tuple<MetaTables.FrnData, Exception>>();
            var frnsToAdd = new Dictionary<string, InstrumentFrn>();
            var frns = notes as IList<MetaTables.FrnData> ?? notes.ToList();

            foreach (var frn in frns) {
                InstrumentFrn instrument = null;
                var failed = false;
                try {
                    instrument = new InstrumentFrn {
                        Cap = frn.Cap,
                        Floor = frn.Floor,
                        Frequency = frn.Frequency,
                        Index = EnsureIndex(Context, frn.IndexRic),
                        Margin = frn.Margin, 
                        InstrumentBond = Context.InstrumentBonds.First(b => b.Ric != null && b.Ric.Name == frn.Ric)
                    };
                } catch (Exception e) {
                    failed = true;
                    res.Add(Tuple.Create(frn, e));
                }

                if (!failed) {
                    if (!useEf) frnsToAdd.Add(frn.Ric, instrument);
                    else Context.InstrumentFrns.Add(instrument);
                }

                try {
                    Context.SaveChanges();
                } catch (DbEntityValidationException e) {
                    Logger.Report("Saving bonds failed", e);
                    throw;
                }
            }

            if (!useEf && frnsToAdd.Any())
                frnsToAdd.Values.ChunkedForEach(x => {
                    var sql = BulkInsertInstrumentFrn(x);
                    //sql = sql.Substring(0, sql.Length - 2);
                    Logger.Info(String.Format("Sql is {0}", sql));
                    Context.Database.ExecuteSqlCommand(sql);
                }, 500);
            return res;
        }

        private Index EnsureIndex(MainEntities ctx, string ind) {
            if (String.IsNullOrWhiteSpace(ind))
                return null;

            var index = 
                ctx.Indices.FirstOrDefault(t => t.Name == ind) ??
                ctx.Indices.Add(new Index {
                    Name = ind, 
                    id_FieldGroup = Fields.GetDefaultFields().id_FieldGroup
                });

            return index;
        }


        private static string BulkInsertInstrumentFrn(IEnumerable<InstrumentFrn> bonds) {
            var res = bonds.Aggregate(
                "INSERT INTO InstrumentFrn(" +
                "id_Bond, id_Index, Cap, Floor, Frequency, Margin" +
                ") SELECT ",
                (current, i) => {
                    var idBond = i.InstrumentBond != null ? i.InstrumentBond.id.ToString(CultureInfo.InvariantCulture) : "NULL";
                    var idIndex = i.Index != null ? i.Index.id.ToString(CultureInfo.InvariantCulture) : "NULL";

                    var cap = i.Cap.HasValue ? "'" + i.Cap.Value.ToString(CultureInfo.InvariantCulture) + "'" : "NULL";
                    var floor = i.Floor.HasValue ? "'" + i.Floor.Value.ToString(CultureInfo.InvariantCulture) + "'" : "NULL";
                    var margin = i.Margin.HasValue ? "'" + i.Margin.Value.ToString(CultureInfo.InvariantCulture) + "'" : "NULL";

                    return current + String.Format("{0}, {1}, {2}, {3}, '{4}', {5} UNION SELECT ", idBond, idIndex, cap, floor, i.Frequency, margin);
                });
            return res.Substring(0, res.Length - " UNION SELECT ".Length);
        }
    }
}