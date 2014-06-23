namespace YieldMap.Database.Access {
    public interface IDbConn {
        MainEntities CreateContext();
    }
}