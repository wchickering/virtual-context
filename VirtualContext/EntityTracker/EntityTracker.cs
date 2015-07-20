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
using System.Data.Linq.Mapping;
using System.Reflection;
using System.ComponentModel;
using System.Data.Linq;
using System.Collections;
using Schrodinger.L2SHelper;

/// TODO: Enhance EntityTracker such that it can accommodate nullable foreign keys.

namespace Schrodinger.EntityTracker
{
    public enum EntityStatus
    {
        Insert,
        Update,
        Delete,
        Original,
    }

    /// <summary>
    /// Maintains state information for entities that implement the INotifyPropertyChanging
    /// and the INotifyPropertyChanged interfaces.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public class EntityTracker<TDataContext> : Schrodinger.EntityTracker.IEntityTracker
        where TDataContext : System.Data.Linq.DataContext
    {
        #region private_variables

        private IORMUtility _ormUtility;

        private bool _isListening;

        // Tracking State Data (comprises totality of tracking context).
        private Dictionary<object, EntityState> _entityStates;

        #endregion private_variables

        #region constructor

        public EntityTracker(IORMUtility ormUtility)
        {
            _ormUtility = ormUtility;
            _isListening = true;

            // Instantiate Tracking State Dictionary using custom IEqualityComparer.
            //_entityStates = new Dictionary<object, EntityState>(new EntityEqualityComparer(_ormUtility));
            _entityStates = new Dictionary<object, EntityState>();
        }

        #endregion constructor

        #region public_properties

        public bool IsListening
        {
            get { return _isListening; }
            set
            {
                if (value)
                {
                    StartListening();
                }
                else
                {
                    StopListening();
                }
            }
        }

        #endregion public_properties

        #region public_methods

        /// <summary>
        /// Detaches any/all objects currently being tracked.
        /// </summary>
        public void Clear()
        {
            foreach (EntityState entityState in EntityStates.ToList())
            {
                DetachEntity(entityState.Entity);
            }
        }

        /// <summary>
        /// Initiate tracking of entity. Entity is initially marked with Status == 'Insert' and IsRoot == false.
        /// </summary>
        /// <param name="entity">entity to track</param>
        public void Track(object entity)
        {
            if ((entity as INotifyPropertyChanging) == null)
            {
                throw new InvalidOperationException("Entity must implement INotifyPropertyChanging.");
            }
            if ((entity as INotifyPropertyChanged) == null)
            {
                throw new InvalidOperationException("Entity must implement INotifyPropertyChanged.");
            }
            if (!_ormUtility.GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Entity Type not recognized.");
            }
            if (IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is already being tracked.");
            }

            // Refresh Tracking Context such that any newly added entities are being tracked.
            RefreshTrackingContext();

            // Register the entity tree.
            foreach (object subEntity in _ormUtility.ToDependentTree(entity))
            {
                EntityState subEntityState = null;
                if (_entityStates.TryGetValue(subEntity, out subEntityState))
                {
                    subEntityState.IsRoot = false;
                }
                else
                {
                    AttachEntity(subEntity);
                }      
            }
            _entityStates[entity].IsRoot = true;
        }

        /// <summary>
        /// Dismiss object graph from EntityTracker.
        /// </summary>
        /// <param name="entity">Root of graph to stop tracking</param>
        public void StopTracking(object entity)
        {
            if (!IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            // Detach all dependents.
            foreach (object subEntity in _ormUtility.ToDependentTree(entity))
            {
                EntityState entityState = null;
                if (_entityStates.TryGetValue(subEntity, out entityState))
                {
                    // Detach any/all former children that marked for deletion.
                    if (entityState.DeletedChildren != null)
                    {
                        foreach (object deletedChild in entityState.DeletedChildren)
                        {
                            if (IsTracking(deletedChild))
                            {
                                DetachEntity(deletedChild);
                            }
                        }
                    }
                    DetachEntity(subEntity);
                }
            }   
        }

        /// <summary>
        /// Indicates whether an entity is currently being tracked.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool IsTracking(object entity)
        {
            return _entityStates.ContainsKey(entity);
        }

        /// <summary>
        /// Marks entire object graph for insertion.
        /// </summary>
        /// <param name="entity">Root of graph to be marked</param>
        public void SetInsert(object entity)
        {
            EntityState entityState = null;
            if (!_entityStates.TryGetValue(entity, out entityState))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            // Ensure any newly added entities are tracked.
            RefreshEntityState(entityState);

            foreach (object subEntity in _ormUtility.ToDependentTree(entity))
            {
                if (_entityStates[subEntity].Status != EntityStatus.Insert)
                {
                    //Call virtual method.
                    MarkInsert(subEntity);
                }
            }
        }

        /// <summary>
        /// Marks entity for update.
        /// </summary>
        /// <param name="entity">Entity to be updated</param>
        public void SetUpdate(object entity)
        {
            if (!IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            if (!_ormUtility.HasPK(entity))
            {
                throw new InvalidOperationException("Cannot mark entity without valid PK as 'Update'.");
            }

            // Call virtual method.
            MarkUpdate(entity);
        }

        /// <summary>
        /// Marks entire object graph for deletion. Any entities in graph previously
        /// marked for insertion are detached from tracking context.
        /// </summary>
        /// <param name="entity">Root of graph to be marked</param>
        public void SetDelete(object entity)
        {
            EntityState entityState = null;
            if (!_entityStates.TryGetValue(entity, out entityState))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }
            if (entityState.Status != EntityStatus.Original && entityState.Status != EntityStatus.Update)
            {
                throw new InvalidOperationException("Cannot delete because entity does not have 'Original' or 'Update' status.");
            }

            foreach (object subEntity in _ormUtility.ToDependentTree(entity))
            {
                EntityState subEntityState = null;
                if (_entityStates.TryGetValue(subEntity, out subEntityState))
                {
                    if (subEntityState.Status == EntityStatus.Insert)
                    {
                        // Canceling insertion.
                        DetachEntity(subEntity);
                    }
                    else
                    {
                        // We will ultimately attach the entity in its original state to the DataContext
                        // since PK composed of FKs may have been changed during implicit deletion.
                        if (!_ormUtility.HasPK(subEntityState.Original))
                        {
                            throw new InvalidOperationException("Cannot mark entity without valid PK as 'Delete'.");
                        }
                        // Call virtual method.
                        MarkDelete(subEntity);
                    }
                }
            }
            _entityStates[entity].IsRoot = true;
        }

        /// <summary>
        /// Marks entire object graph to be "attached" as existing and unmodified.
        /// All entities in graph must have valid PKs.
        /// </summary>
        /// <param name="entity">Root of graph to be marked</param>
        public void SetOriginal(object entity)
        {
            if (!IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            foreach (object subEntity in _ormUtility.ToDependentTree(entity))
            {
                if (!IsTracking(subEntity))
                {
                    throw new InvalidOperationException("Object graph has been augmented since tracking began.");
                }
                if (!_ormUtility.HasPK(subEntity))
                {
                    throw new InvalidOperationException("Cannot mark entity without valid PK as 'Original'.");
                }

                // Initialize Original Entity reference.
                _entityStates[subEntity].Original = _ormUtility.ShallowCopy(subEntity);

                // Delete any/all DeletedChildren references.
                _entityStates[subEntity].DeletedChildren = null;

                // Call virtual method.
                MarkOriginal(subEntity);
            }
        }

        /// <summary>
        /// Sets the current entity status.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="entityStatus"></param>
        public void SetEntityStatus(object entity, EntityStatus entityStatus)
        {
            if (!IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            // Call corresponding virtual method.
            switch (entityStatus)
            { 
                case EntityStatus.Insert:
                    MarkInsert(entity);
                    break;
                case EntityStatus.Update:
                    if (!_ormUtility.HasPK(entity))
                    {
                        throw new InvalidOperationException("Cannot mark entity without valid PK as 'Update'.");
                    }
                    MarkUpdate(entity);
                    break;
                case EntityStatus.Delete:
                    if (!_ormUtility.HasPK(entity))
                    {
                        throw new InvalidOperationException("Cannot mark entity without valid PK as 'Delete'.");
                    }
                    MarkDelete(entity);
                    break;
                case EntityStatus.Original:
                    if (!_ormUtility.HasPK(entity))
                    {
                        throw new InvalidOperationException("Cannot mark entity without valid PK as 'Original'.");
                    }
                    MarkOriginal(entity);
                    break;
            }
        }

        /// <summary>
        /// Gets the current entity status.
        /// </summary>
        /// <param name="entity">entity to check</param>
        /// <returns></returns>
        public EntityStatus GetEntityStatus(object entity)
        {
            if (!IsTracking(entity))
            {
                throw new InvalidOperationException("Entity is not being tracked.");
            }

            return _entityStates[entity].Status;
        }

        public IEnumerable<EntityState> EntityStates
        {
            get
            {
                RefreshTrackingContext();
                return _entityStates.Values;
            }
        }

        /// <summary>
        /// Checks all tracked entities for untracked dependents. Any previously untracked
        /// dependents are attached for tracking and marked for insertion.
        /// </summary>
        public void RefreshTrackingContext()
        {
            List<EntityState> entityStateList = _entityStates.Values.ToList();
            foreach (EntityState entityState in entityStateList)
            {
                // Look for untracked descendents of a tracked root entity.
                if (entityState.IsRoot)
                {
                    RefreshEntityState(entityState);
                }
            }
        }

        /// <summary>
        /// Marks all tracked entities, not previously marked as 'Delete' or 'Locked',
        /// as 'Original'. Entities previously marked as 'Delete' are removed from the
        /// tracking context. This method should be called following synchronization with
        /// a DataContext.
        /// </summary>
        public void ResetTracking()
        {
            List<EntityState> entityStateList = _entityStates.Values.ToList();

            foreach (EntityState entityState in entityStateList)
            {
                if (entityState.IsRoot)
                {
                    if (entityState.Status == EntityStatus.Delete)
                    {
                        StopTracking(entityState.Entity);
                    }
                    else
                    {
                        SetOriginal(entityState.Entity);
                    }
                }
            }
        }

        #endregion public_methods

        #region virtual_methods

        // The following methods are provided so that EntityTracker can be extended and specialized.

        protected virtual void MarkTracking(object entity)
        {
        }

        protected virtual void MarkStopTracking(object entity)
        {
        }

        protected virtual void MarkInsert(object entity)
        {
            _entityStates[entity].Status = EntityStatus.Insert;
        }

        protected virtual void MarkUpdate(object entity)
        {
            _entityStates[entity].Status = EntityStatus.Update;
        }

        protected virtual void MarkDelete(object entity)
        {
            _entityStates[entity].Status = EntityStatus.Delete;
        }

        protected virtual void MarkOriginal(object entity)
        {
            _entityStates[entity].Status = EntityStatus.Original;
        }

        #endregion virtual_methods

        #region private_methods

        /// <summary>
        /// Handles the PropertyChanging event sent by a tracked entity.
        /// </summary>
        /// <param name="sender">Tracked entity</param>
        /// <param name="e">PropertyChanging arguments</param>
        protected void PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            EntityState entityState = null;
            if (!_entityStates.TryGetValue(sender, out entityState))
            {
                throw new InvalidOperationException("Failed to locate entity within internal dictionary.");
            }

            if (entityState.Status == EntityStatus.Delete)
            {
                throw new InvalidOperationException("Cannot modify entity marked as 'Delete'.");
            }
        }

        /// <summary>
        /// Handles the PropertyChanged event sent by a tracked entity.
        /// 
        /// </summary>
        /// <param name="sender">Tracked entity</param>
        /// <param name="e">PropertyChanged arguments</param>
        protected void PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            EntityState entityState = null;
            if (!_entityStates.TryGetValue(sender, out entityState))
            {
                throw new InvalidOperationException("Failed to locate entity within internal dictionary.");
            }

            PropertyInfo property = null;

            // Check for Fk Property change.
            if ((property = _ormUtility.GetForeignKeyRef(sender.GetType(), e.PropertyName)) != null)
            {
                // FK set to null.
                if (property.GetValue(sender, null) == null)
                {
                    // Disconnected from tracking context.
                    if (!entityState.IsRoot && !IsConnectedAsChild(sender))
                    {
                        // Canceling insertion.
                        if (entityState.Status == EntityStatus.Insert)
                        {
                            StopTracking(sender);
                        }
                        // Pre-existing entity has been disconnected from graph implying deletion.
                        else
                        {
                            DeleteFromGraph(entityState, property);
                        }
                    }
                    // FK removed but still attached to tracking context.
                    else
                    {
                        EntityChanged(entityState);
                    }
                }
                // FK value added or changed.
                else
                {
                    // Do not allow entities to be orphaned in this way.
                    if (!entityState.IsRoot && !IsConnectedAsChild(sender))
                    {
                        throw new InvalidOperationException("Entity detached from tracking context by overwriting tracked FK with non-tracked FK.");
                    }

                    // Entity previously a root but now connected as a child.
                    if (entityState.IsRoot && IsConnectedAsChild(sender))
                    {
                        entityState.IsRoot = false;
                    }
                    EntityChanged(entityState);
                }
            }
            // Do nothing if DB generated property was changed.
            else if ((property = _ormUtility.GetDBGenProperty(sender.GetType(), e.PropertyName)) != null)
            {
                ;
            }
            // Some other DB column property changed was changed.
            else
            {
                EntityChanged(entityState);
            }
        }

        /// <summary>
        /// Binds change tracking event handlers to all entities within tracking context.
        /// </summary>
        private void StartListening()
        {
            _isListening = true;
            foreach (object entity in _entityStates.Keys)
            {
                StartListeningToEntity(entity);
            }
        }

        /// <summary>
        /// Removes vhange tracking event handlers from all entities within tracking context.
        /// </summary>
        private void StopListening()
        {
            _isListening = false;
            foreach (object entity in _entityStates.Keys)
            {
                StopListeningToEntity(entity);
            }
        }

        /// <summary>
        /// Binds change tracking event handlers to an entity.
        /// </summary>
        /// <param name="entity"></param>
        protected void StartListeningToEntity(object entity)
        {
            // Bind Change events.
            ((INotifyPropertyChanging)entity).PropertyChanging += new PropertyChangingEventHandler(PropertyChanging);
            ((INotifyPropertyChanged)entity).PropertyChanged += new PropertyChangedEventHandler(PropertyChanged);
        }

        /// <summary>
        /// Removes change tracking event handlers from an entity.
        /// </summary>
        /// <param name="entity"></param>
        protected void StopListeningToEntity(object entity)
        {
            // Remove event handlers.
            ((INotifyPropertyChanging)entity).PropertyChanging -= new PropertyChangingEventHandler(PropertyChanging);
            ((INotifyPropertyChanged)entity).PropertyChanged -= new PropertyChangedEventHandler(PropertyChanged);
        }

        /// <summary>
        /// Bind change tracking event handlers, create EntityState object with default Status == 'Insert',
        /// and insert into internal dictionary. 
        /// </summary>
        /// <param name="entity">Entity to attach</param>
        private void AttachEntity(object entity)
        {
            // Create EntityState proxy object.
            EntityState entityState = new EntityState(entity);
            entityState.Status = EntityStatus.Insert; // We default to 'Insert' b/c 'Original' entities must have valid PK.
            entityState.Original = _ormUtility.ShallowCopy(entity);

            if (IsListening)
            {
                // Bind event handlers.
                StartListeningToEntity(entity);
            }

            // Add to proxy collection.
            _entityStates.Add(entity, entityState);

            // Call virtual method.
            MarkTracking(entity);
        }

        /// <summary>
        /// Remove change tracking event handlers and erase from internal dictionary.
        /// </summary>
        /// <param name="entity">Entity to detach</param>
        private void DetachEntity(object entity)
        {
            // Call virtual method.
            MarkStopTracking(entity);

            if (IsListening)
            {
                // Remove event handlers.
                StopListeningToEntity(entity);
            }

            // Remove from internal dictionary.
            _entityStates.Remove(entity);
        }

        /// <summary>
        /// Marks entity for deletion and associates deleted entity with EntityState
        /// of former FK entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="fkProperty"></param>
        private void DeleteFromGraph(EntityState entityState, PropertyInfo fkProperty)
        {
            SetDelete(entityState.Entity);

            // Associate deleted entity with EntityState of former FK entity.
            PropertyInfo fkThisKey = _ormUtility.GetFKThisKey(fkProperty);
            PropertyInfo fkOtherKey = _ormUtility.GetFKOtherKey(fkProperty);
            object fkEntity = null;
            IEnumerable<object> candidates = null;

            if ((candidates = _entityStates.Keys.Where(o => o.GetType() == fkProperty.PropertyType)) != null &&
                (fkEntity = candidates.Where(c =>
                    Equals(fkOtherKey.GetValue(c, null),
                           fkThisKey.GetValue(entityState.Original, null))).SingleOrDefault()) != null)
            {
                EntityState parentEntityState = _entityStates[fkEntity];
                if (parentEntityState.DeletedChildren == null)
                {
                    parentEntityState.DeletedChildren = new List<object>();
                }
                parentEntityState.DeletedChildren.Add(entityState.Entity);
            }
        }

        /// <summary>
        /// Updates EntityState of changed entity depending upon previous EntityState.
        /// </summary>
        /// <param name="entityState">EntityState of changed entity</param>
        private void EntityChanged(EntityState entityState)
        {
            // Original Entity has been changed.
            if (entityState.Status == EntityStatus.Original && !_ormUtility.ShallowCompare(entityState.Entity, entityState.Original))
            {
                SetUpdate(entityState.Entity);
            }
            // Changed back to original state.
            else if (entityState.Status == EntityStatus.Update && _ormUtility.ShallowCompare(entityState.Entity, entityState.Original))
            {
                SetOriginal(entityState.Entity);
            }
        }

        /// <summary>
        /// This method checks for untracked dependents. Any previously untracked dependents
        /// are attached for tracking and marked for insertion.
        /// </summary>
        /// <param name="entityState">EntityState to Refresh</param>
        protected void RefreshEntityState(EntityState entityState)
        {
            foreach (object subEntity in _ormUtility.ToDependentTree(entityState.Entity))
            {
                // Begin Tracking and Mark for insert any previously untracked dependents.
                if (!IsTracking(subEntity))
                {
                    AttachEntity(subEntity);
                }
            }
        }

        /// <summary>
        /// Determines whether or not entity is presently "attached" to tracking context as
        /// a dependent of another attached entity (i.e. is not root).
        /// </summary>
        /// <param name="entity">entity to check</param>
        /// <returns>
        /// Returns true if and only if it is associated with a FK that is being tracked and has
        /// EntityStatus != 'Delete'. Otherwise, returns false.
        /// </returns>
        private bool IsConnectedAsChild(object entity)
        {
            if (IsTracking(entity))
            {
                foreach (PropertyInfo fkProperty in _ormUtility.GetForeignKeyRefs(entity.GetType()))
                {
                    object fkEntity = null;
                    if ((fkEntity = fkProperty.GetValue(entity, null)) != null)
                    {
                        EntityState fkState = null;
                        if (_entityStates.TryGetValue(fkEntity, out fkState))
                        {
                            if (fkState.Status != EntityStatus.Delete /*&& fkState.Status != EntityStatus.Locked */)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        #endregion private_methods
  
    }
}
