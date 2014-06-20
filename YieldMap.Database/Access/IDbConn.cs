using YieldMap.Database.StoredProcedures.Additions;

namespace YieldMap.Database.Access {
    public interface IDbConn {
        MainEntities CreateContext();
        Bonds CreateBonds();
        ChainRics CreateRics();
        Ratings CreateRatings();
    }
}