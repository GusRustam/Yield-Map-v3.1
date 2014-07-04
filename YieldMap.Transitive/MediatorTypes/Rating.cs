using System;
using System.IO;
using YieldMap.Requests.MetaTables;

namespace YieldMap.Transitive.MediatorTypes {
    public class Rating {
        public bool Issuer { get; private set; }
        public string RatingName { get; private set; }
        public string Source { get; private set; }
        public DateTime Date { get; private set; }
        public string Ric { get; private set; }

        private Rating() {}

        public static Rating Create(MetaTables.IssueRatingData data) {
            if (data.RatingDate != null) {
                return new Rating {
                    Issuer = false,
                    RatingName = data.Rating,
                    Source = data.RatingSourceCode,
                    Date = data.RatingDate.Value,
                    Ric = data.Ric
                };
            }
            throw new InvalidDataException();
        }

        public static Rating Create(MetaTables.IssuerRatingData data) {
            if (data.RatingDate != null) {
                return new Rating {
                    Issuer = true,
                    RatingName = data.Rating,
                    Source = data.RatingSourceCode,
                    Date = data.RatingDate.Value,
                    Ric = data.Ric
                };
            }
            throw new InvalidDataException();
        }
    }
}
