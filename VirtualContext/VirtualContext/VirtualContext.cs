/// Copyright (C) 2015 Bill Chickering
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU General Public License as published by
/// the Free Software Foundation, either version 3 of the License, or
/// (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU General Public License for more details.
///
/// You should have received a copy of the GNU General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Schrodinger.EntityTracker;
using Schrodinger.EntityFactory;
using Schrodinger.L2SHelper;

namespace Schrodinger.VirtualContext
{
    /// <summary>
    /// The Dynamic Data Context, or DDC, provides powerful querying/entity retrieval along with "detached" change tracking.
    /// 
    /// The DDC works with Linq to Sql to provide a complete data access layer.
    /// 
    /// The DDC simplifies retrieval along with creation, updating, and destruction of complete object graphs.
    /// 
    /// The DDC is not a framework, and can live side by side with other data access solutions.
    /// 
    /// The DDC integrates well with both ASP.NET Web Forms and MVC.
    /// 
    /// The DDC uses method chaining to provide a fully fluent interface for L2S entity retrieval and querying.
    /// 
    /// The DDC's Object Relational Mapping Utility, or ORMUtility, exposes a rich collection of methods for manipulating
    /// object graphs. This includes comparisons, duplication, serialization, working with or without an entity's children and
    /// parent objects. Moreover, using the ORMUtility to manipulate an application's entities offers the added benefit of
    /// provides the application developer indiaccess to the DDC's optimized 
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public class VirtualContext<TDataContext> : IEntityTracker, IEntityFactory<TDataContext>
        where TDataContext : System.Data.Linq.DataContext, new()
    {
        #region private_variables

        private IORMUtility _ormUtility;
        private IEntityTracker _entityTracker;
        private IEntityFactory<TDataContext> _entityFactory;

        private TextWriter _log;

        #endregion private_variables

        #region constructor

        /// <summary>
        /// Instantiate necessary resources:
        /// ORMUtility provides helper methods for working with the object model.
        /// EntityTracker provides "detached" change tracking.
        /// EntityFactory provides DB querying and object retrieval functionality.
        /// </summary>
        public VirtualContext()
        {
            // Dependency injection for ORMUtility, EntityTracker, and EntityFactory.
            _ormUtility = new L2SUtility<TDataContext>();
            _entityTracker = new EntityTracker<TDataContext>(_ormUtility);
            _entityFactory = new EntityFactory<TDataContext>(_ormUtility, _entityTracker);
        }

        #endregion constructor

        #region public_methods

        /// <summary>
        /// This property is passed along to all instances of DataContext created
        /// directly or indirectly by the VirtualContext.
        /// </summary>
        public TextWriter Log
        {
            get { return _log; }
            set
            {
                _log = value;
                _entityFactory.Log = _log;
            }
        }

        /// <summary>
        /// Expose the VirtualContext's ORMUtility.
        /// </summary>
        public IORMUtility ORMUtility
        {
            get { return _ormUtility; }
        }

        /// <summary>
        /// Use a DataSyncTool, which is directed by the EntityTracker, to synchronize the
        /// working context with an actual DataContext. Then invoke DataContext.SubmitChanges().
        /// </summary>
        public void SubmitChanges()
        {
            using (TDataContext dataContext = new TDataContext())
            {
                dataContext.DeferredLoadingEnabled = false;
                if (_log != null)
                {
                    dataContext.Log = _log;
                }

                // DataSyncTool dependency injection.
                IDataSyncTool dataContextSyncTool = new L2SDataSyncTool<TDataContext>(dataContext, _ormUtility, _entityTracker);
                
                // Synchronize with DataContext.
                dataContextSyncTool.Sync();
   
                if (_log != null)
                {
                    _log.WriteLine("---- Invoking dataContext.SubmitChanges() ----");
                }
                _entityTracker.IsListening = false; // Disable event handling by EntityTracker.
                try
                {
                    dataContext.SubmitChanges();
                }
                finally
                {
                    _entityTracker.IsListening = true; // Re-enable event handling by EntityTracker.
                    if (_log != null)
                    {
                        _log.WriteLine("----------------------------------------------");
                    }
                }
                
                // 'Reset' entity states within EntityTracker.
                _entityTracker.ResetTracking();
            }
        }

        #region IEntityFactory_interface

        /// <summary>
        /// With respect to querying and data/object retrieval, the VirtualContext is merely a wrapper for
        /// the EntityFactory.
        /// </summary>
        
        public EntityQuery<TDataContext, TEntity> Get<TEntity>() where TEntity : class { return _entityFactory.Get<TEntity>(); }
        public EntityQuery<TDataContext> Get(Type type) { return _entityFactory.Get(type); }
        public EntityQuery<TDataContext, TEntity> GetCount<TEntity>() where TEntity : class { return _entityFactory.GetCount<TEntity>(); }
        public EntityQuery<TDataContext> GetCount(Type type) { return _entityFactory.GetCount(type); }
        public void GetFKs<TEntity>(TEntity entity) where TEntity : class { _entityFactory.GetFKs<TEntity>(entity); }
        public void GetFKs(object entity) { _entityFactory.GetFKs(entity); }
        public void Load(DataRequest dataRequest) { _entityFactory.Load(dataRequest); }
        public void Count(DataRequest dataRequest) { _entityFactory.Count(dataRequest); }
        public void LoadPending() { _entityFactory.LoadPending(); }

        #endregion IEntityFactory_interface

        #region IEntityTracker_interface

        /// <summary>
        /// With respect to change tracking, the VirtualContext is merely a wrapper for the EntityTracker. 
        /// </summary>

        public void Clear() { _entityTracker.Clear(); }
        public System.Collections.Generic.IEnumerable<EntityState> EntityStates { get { return _entityTracker.EntityStates; } }
        public EntityStatus GetEntityStatus(object entity) { return _entityTracker.GetEntityStatus(entity); }
        //public bool IsFKLocking { get { return _entityTracker.IsFKLocking; } set { _entityTracker.IsFKLocking = value; } }
        public bool IsListening { get { return _entityTracker.IsListening; } set { _entityTracker.IsListening = value; } }
        public bool IsTracking(object entity) { return _entityTracker.IsTracking(entity); }
        //public void Lock(object entity) { _entityTracker.Lock(entity); }
        public void RefreshTrackingContext() { _entityTracker.RefreshTrackingContext(); }
        public void ResetTracking() { _entityTracker.ResetTracking(); }
        public void SetDelete(object entity) { _entityTracker.SetDelete(entity); }
        public void SetEntityStatus(object entity, EntityStatus entityStatus) { _entityTracker.SetEntityStatus(entity, entityStatus); }
        public void SetInsert(object entity) { _entityTracker.SetInsert(entity); }
        //public void SetLocked(object entity) { _entityTracker.SetLocked(entity); }
        public void SetOriginal(object entity) { _entityTracker.SetOriginal(entity); }
        public void SetUpdate(object entity) { _entityTracker.SetUpdate(entity); }
        public void StopTracking(object entity) { _entityTracker.StopTracking(entity); }
        public void Track(object entity) { _entityTracker.Track(entity); }
        //public void Unlock(object entity) { _entityTracker.Unlock(entity); }

        #endregion IEntityTracker_interface

        #endregion public_methods
    }
}
