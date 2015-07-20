using System;
namespace Schrodinger.EntityTracker
{
    public interface IEntityTracker
    {
        void Clear();
        System.Collections.Generic.IEnumerable<EntityState> EntityStates { get; }
        EntityStatus GetEntityStatus(object entity);
        bool IsListening { get; set; }
        bool IsTracking(object entity);
        void RefreshTrackingContext();
        void ResetTracking();
        void SetDelete(object entity);
        void SetEntityStatus(object entity, EntityStatus entityStatus);
        void SetInsert(object entity);
        void SetOriginal(object entity);
        void SetUpdate(object entity);
        void StopTracking(object entity);
        void Track(object entity);
    }
}
