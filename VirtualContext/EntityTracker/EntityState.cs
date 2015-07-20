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
using System.Runtime.Serialization;

namespace Schrodinger.EntityTracker
{
    /// <summary>
    /// Simple data class (essentially a struct) that houses tracking state for an entity.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    [DataContract()]
    public class EntityState
    {

        #region private_variables

        protected object _entity;
        protected object _original;
        protected EntityStatus _entityStatus;
        protected bool _isRoot;
        protected IList<object> _deletedChildren;

        #endregion private_variables

        #region constructor

        public EntityState(object entity)
        {
            _entity = entity;
            _entityStatus = EntityStatus.Original;
            _isRoot = false;
        }

        #endregion constructor

        #region public_methods

        [DataMember(Order = 1, EmitDefaultValue = false)]
        public object Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        [DataMember(Order = 2)]
        public object Original
        {
            get { return _original; }
            set { _original = value; }
        }

        [DataMember(Order = 3)]
        public EntityStatus Status
        {
            get { return _entityStatus; }
            set { _entityStatus = value; }
        }

        [DataMember(Order = 4)]
        public bool IsRoot
        {
            get { return _isRoot; }
            set { _isRoot = value; }
        }

        [DataMember(Order = 4)]
        public IList<object> DeletedChildren
        {
            get { return _deletedChildren; }
            set { _deletedChildren = value; }
        }

        #endregion public_methods
    }
}
