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
