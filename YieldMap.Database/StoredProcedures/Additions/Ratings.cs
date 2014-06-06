using System;
using System.Collections.Generic;
using System.Linq;
using YieldMap.Database.Access;
using YieldMap.Database.Tools;
using YieldMap.Tools.Logging;

namespace YieldMap.Database.StoredProcedures.Additions {
    public class Ratings {
        private static readonly Logging.Logger Logger = Logging.LogFactory.create("Database.Additions.Ratings");

        public void SaveRatings(IEnumerable<Transitive.Rating> ratings) {
            var rtis = new Dictionary<Tuple<long, long, DateTime>, RatingToInstrument>();
            var rtcs = new Dictionary<Tuple<long, long, DateTime>, RatingToLegalEntity>();

            using (var context = DbConn.CreateContext()) {
                var local = context;
                var enumerable = ratings as Transitive.Rating[] ?? ratings.ToArray();

                var instruments = (
                    from rating in enumerable
                    let r = rating
                    where !r.Issuer
                    let ratingInfo = local.RatingsViews.FirstOrDefault(x => x.AgencyCode == r.Source && x.RatingName == r.RatingName)
                    where ratingInfo != null
                    let ricInfo = local.InstrumentRicViews.FirstOrDefault(x => x.Name == rating.Ric)
                    where ricInfo != null
                    select new RatingToInstrument { id_Instrument = ricInfo.id_Instrument, id_Rating = ratingInfo.id_Rating, RatingDate = rating.Date}).ToList();

                foreach (var instrument in instruments) {
                    if (instrument.RatingDate == null || instrument.id_Instrument == null) continue;
                    var key = Tuple.Create(instrument.id_Instrument.Value, instrument.id_Rating, instrument.RatingDate.Value);
                    if (!rtis.ContainsKey(key)) rtis.Add(key, instrument);
                }

                var companies = (
                    from rating in enumerable
                    let r = rating
                    where r.Issuer
                    let ratingInfo = local.RatingsViews.FirstOrDefault(x => x.AgencyCode == r.Source && x.RatingName == r.RatingName)
                    where ratingInfo != null
                    let ricInfo = local.InstrumentIBViews.FirstOrDefault(x => x.Name == rating.Ric)
                    where ricInfo != null && (ricInfo.id_Issuer.HasValue || ricInfo.id_Borrower.HasValue)
                    let idLegalEntity = ricInfo.id_Issuer.HasValue ? ricInfo.id_Issuer.Value : ricInfo.id_Borrower.Value
                    select new RatingToLegalEntity { id_LegalEntity = idLegalEntity, id_Rating = ratingInfo.id_Rating, RatingDate = rating.Date }).ToList();
                
                foreach (var company in companies) {
                    if (company.RatingDate == null || company.id_LegalEntity == null) continue;
                    var key = Tuple.Create(company.id_LegalEntity.Value, company.id_Rating, company.RatingDate.Value);
                    if (!rtcs.ContainsKey(key)) rtcs.Add(key, company);
                }                
                var peggedContext = local;
                if (rtis.Any())
                    rtis.Values.ChunkedForEach(x => {
                        var sql = BulkInsertRatingLink(x);
                        Logger.Info(String.Format("Sql is {0}", sql));
                        peggedContext.Database.ExecuteSqlCommand(sql);
                    }, 500);
                if (rtcs.Any())
                    rtcs.Values.ChunkedForEach(x => {
                        var sql = BulkInsertRatingLink2(x);
                        Logger.Info(String.Format("Sql is {0}", sql));
                        peggedContext.Database.ExecuteSqlCommand(sql);
                    }, 500);
            }
        }

        private static string BulkInsertRatingLink(IEnumerable<RatingToInstrument> ratings) {
            var res = ratings.Aggregate(
                "INSERT INTO RatingToInstrument(id_Rating, id_Instrument, RatingDate) VALUES",
                (current, i) => {
                    var date = i.RatingDate.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.RatingDate.Value.ToLocalTime()) : "NULL";
                    return current + String.Format("({0}, {1}, {2}), ", i.id_Rating, i.id_Instrument, date);
                });
            return res.Substring(0, res.Length - 2);
        }


        private static string BulkInsertRatingLink2(IEnumerable<RatingToLegalEntity> ratings) {
            var res = ratings.Aggregate(
                "INSERT INTO RatingToLegalEntity(id_Rating, id_LegalEntity, RatingDate) VALUES",
                (current, i) => {
                    var date = i.RatingDate.HasValue ? String.Format("\"{0:yyyy-MM-dd 00:00:00}\"", i.RatingDate.Value.ToLocalTime()) : "NULL";
                    return current + String.Format("({0}, {1}, {2}), ", i.id_Rating, i.id_LegalEntity, date);
                });
            return res.Substring(0, res.Length - 2);
        }
    }
}
