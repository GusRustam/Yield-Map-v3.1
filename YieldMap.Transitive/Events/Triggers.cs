namespace YieldMap.Transitive.Events {
    public static class Triggers {
        public static ITriggerManager Main { get; private set; }

        public static void Initialize(ITriggerManager main) {
            Main = main;
        }

        static Triggers() {
            var chainRicHander = new ChainRicHandler(null);
            Main = new InstrumentDescriptionHandler(chainRicHander);
        }
    }
}