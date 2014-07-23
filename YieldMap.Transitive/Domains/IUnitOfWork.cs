using System;
using System.Data.Entity;
using YieldMap.Transitive.Events;

namespace YieldMap.Transitive.Domains {
    public interface IUnitOfWork<out TContext> : IDisposable where TContext : DbContext {
        event EventHandler<IDbEventArgs> Notify;
        int Save();
        TContext Context { get; }
    }
}