namespace YieldMap.Database.Access {
    public abstract class AccessToDb  {
        protected MainEntities Context;

        protected AccessToDb(MainEntities context) {
            Context = context;
        }

        protected AccessToDb() {
            Context = DbConn.CreateContext();
        }

    }
}