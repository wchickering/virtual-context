using System;
namespace Schrodinger.EntityFactory
{
    public interface IEntityFactory<TDataContext>
     where TDataContext : System.Data.Linq.DataContext, new()
    {
        void Count(DataRequest dataRequest);
        EntityQuery<TDataContext> Get(Type type);
        EntityQuery<TDataContext, TEntity> Get<TEntity>() where TEntity : class;
        EntityQuery<TDataContext> GetCount(Type type);
        EntityQuery<TDataContext, TEntity> GetCount<TEntity>() where TEntity : class;
        void GetFKs(object entity);
        void GetFKs<TEntity>(TEntity entity) where TEntity : class;
        void Load(DataRequest dataRequest);
        void LoadPending();
        System.IO.TextWriter Log { get; set; }
    }
}
