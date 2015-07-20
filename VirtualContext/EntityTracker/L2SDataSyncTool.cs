using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Schrodinger.L2SHelper;
using System.Data.Linq;

namespace Schrodinger.EntityTracker
{
    /// <summary>
    /// Synchronizes entities with respect to a L2S DataContext according to their states
    /// maintained within an IEntityTracker.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public class L2SDataSyncTool<TDataContext> : Schrodinger.EntityTracker.IDataSyncTool
        where TDataContext : System.Data.Linq.DataContext
    {

        #region private_variables

        private TDataContext _dataContext;
        private IEntityTracker _entityTracker;
        private IORMUtility _ormUtility;

        #endregion private_variables

        #region constructor

        public L2SDataSyncTool(TDataContext dataContext, IORMUtility ormUtility, IEntityTracker entityTracker)
        {
            if (dataContext == null || ormUtility == null || entityTracker == null)
            {
                throw new ArgumentNullException();
            }
            _dataContext = dataContext;
            _ormUtility = ormUtility;
            _entityTracker = entityTracker;
        }

        #endregion constructor

        #region public_methods

        public void Sync()
        {
            if (_dataContext.DeferredLoadingEnabled)
            {
                throw new InvalidOperationException("Target DataContext must have DeferredLoading disabled.");
            }

            // Disable event handling by EntityTracker.
            _entityTracker.IsListening = false;

            foreach (EntityState entityState in _entityTracker.EntityStates)
            {
                SyncEntityState(entityState);
            }

            // Re-enable event handling by EntityTracker.
            _entityTracker.IsListening = true;
        }

        #endregion public_methods

        #region private_methods

        private void SyncEntityState(EntityState entityState)
        {
            switch (entityState.Status)
            {
                // Entity marked as 'Original'.
                case EntityStatus.Original:
                    if (!_ormUtility.HasPK(entityState.Entity))
                    {
                        throw new InvalidOperationException("Cannot attach entity as 'Original' without valid PK.");
                    }
                    _dataContext.GetTable(entityState.Entity.GetType()).Attach(entityState.Entity);
                    break;

                // Entity marked as 'Insert'.
                case EntityStatus.Insert:
                    // L2S will attempt to insert any unattached FKs--therefore, we attach all untracked FKs.
                    foreach (PropertyInfo fkProperty in _ormUtility.GetForeignKeyRefs(entityState.Entity.GetType()))
                    {
                        object fkEntity = fkProperty.GetValue(entityState.Entity, null);
                        if (fkEntity != null)
                        {
                            if (!_entityTracker.IsTracking(fkEntity))
                            {
                                if (!_ormUtility.HasPK(fkEntity))
                                {
                                    throw new InvalidOperationException("Untracked FK does not have valid PK.");
                                }
                                try
                                {
                                    _dataContext.GetTable(fkEntity.GetType()).Attach(fkEntity);
                                }
                                catch
                                {
                                    // Ignore this error. FK has already been attached.
                                    // It is unfortunate, but L2S provides no way to check whether an object
                                    // is already attached to a DataContext.
                                }
                            }
                        }
                    }
                    _dataContext.GetTable(entityState.Entity.GetType()).InsertOnSubmit(entityState.Entity);
                    break;

                // Entity marked as 'Update'.
                case EntityStatus.Update:
                    if (!_ormUtility.HasPK(entityState.Entity))
                    {
                        throw new InvalidOperationException("Cannot attach entity for 'Update' without valid PK.");
                    }
                    if (_ormUtility.ShallowCompare(entityState.Entity, entityState.Original))
                    {
                        _dataContext.GetTable(entityState.Entity.GetType()).Attach(entityState.Entity, true);
                    }
                    else
                    {
                        _dataContext.GetTable(entityState.Entity.GetType()).Attach(entityState.Entity, entityState.Original);
                    }
                    break;

                // Entity marked as 'Delete'.
                case EntityStatus.Delete:
                    // It's important to attach the entity in its original state since PK composed of FKs
                    // may have been changed during implicit deletion.
                    if (!_ormUtility.HasPK(entityState.Original))
                    {
                        throw new InvalidOperationException("Cannot attach entity for 'Delete' without valid PK.");
                    }
                    _dataContext.GetTable(entityState.Entity.GetType()).Attach(entityState.Original);
                    _dataContext.GetTable(entityState.Entity.GetType()).DeleteOnSubmit(entityState.Original);
                    break;
            }
        }

        #endregion private_methods
    }
}
