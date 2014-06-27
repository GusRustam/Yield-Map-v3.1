using System;
using YieldMap.Language;
using YieldMap.Tools;

namespace YieldMap.Database.Functions.Outcome {
    public class Computed {
        private readonly Lexan.Value _val;

        public Computed(Lexan.Value val) {
            _val = val;
        }

        public OutcomeType OutcomeType {
            get {
                if (_val.IsBool) return OutcomeType.Bool;
                if (_val.IsDate) return OutcomeType.Date;
                if (_val.IsDouble) return OutcomeType.Double;
                if (_val.IsInteger) return OutcomeType.Integer;
                if (_val.IsNothing) return OutcomeType.Nothing;
                if (_val.IsRating) return OutcomeType.Rating;
                if (_val.IsString) return OutcomeType.String;
                throw new ArgumentOutOfRangeException();
            }
        }

        public bool BoolValue { get { return ((Lexan.Value.Bool)_val).Item; } }
        public DateTime DateValue { get { return ((Lexan.Value.Date)_val).Item; } }
        public double DoubleValue { get { return ((Lexan.Value.Double)_val).Item; } }
        public long IntegerValue { get { return ((Lexan.Value.Integer)_val).Item; } }
        public Ratings.Notch RatingValue { get { return ((Lexan.Value.Rating)_val).Item; } }
        public string StringValue { get { return ((Lexan.Value.String)_val).Item; } }

        public object Value {
            get {
                switch (OutcomeType) {
                    case OutcomeType.Integer: return IntegerValue;
                    case OutcomeType.Double: return DoubleValue;
                    case OutcomeType.String: return StringValue;
                    case OutcomeType.Rating: return RatingValue;
                    case OutcomeType.Date: return DateValue;
                    case OutcomeType.Bool: return BoolValue;
                    case OutcomeType.Nothing: return null;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}