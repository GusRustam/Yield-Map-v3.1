namespace YieldMap.Database {
    public partial class Feed : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Instrument : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Description : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Country : IObjectWithState {
        public State State { get; set; }
    }
    public partial class LegalEntity : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Ticker : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Industry : IObjectWithState {
        public State State { get; set; }
    }
    public partial class SubIndustry : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Specimen : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Seniority : IObjectWithState {
        public State State { get; set; }
    }
    public partial class InstrumentType : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Chain : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Ric : IObjectWithState {
        public State State { get; set; }
    }
    public partial class RicToChain : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Index : IObjectWithState {
        public State State { get; set; }
    }
    public partial class Property : IObjectWithState {
        public State State { get; set; }
    }
    public partial class PropertyValue : IObjectWithState {
        public State State { get; set; }
    }
}
