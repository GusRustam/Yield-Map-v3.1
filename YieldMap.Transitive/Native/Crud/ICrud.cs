using System;
using System.Collections.Generic;
using YieldMap.Transitive.Events;
using YieldMap.Transitive.Native.Entities;

namespace YieldMap.Transitive.Native.Crud {
    public interface ICrud<T> : IDisposable where T : class, IIdentifyable, IEquatable<T>  {
        event EventHandler<IDbEventArgs> Notify;

        // CRUD functions
        void Create(T item);
        void Update(T item);
        void Delete(T item);
        void Save<TEntity>() where TEntity : class, IIdentifyable, IEquatable<TEntity>;

        /// <summary>
        /// Does not wait for Save to be called!  Use with care!
        /// </summary>
        void DeleteAll();

        // Events functions
        void MuteOnce();
        void Mute();
        void Unmute();
        bool Muted();

        // Reader functions
        IEnumerable<T> FindAll();
        IEnumerable<T> FindBy(Func<T, bool> predicate);
        T FindById(long id);
    }
}
