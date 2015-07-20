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
using System.Reflection;
using Schrodinger.L2SHelper;

namespace Schrodinger.EntityTracker
{
    //public class EntityEqualityComparer : IEqualityComparer<object>
    //{
    //    private IORMUtility _ormUtility;

    //    public EntityEqualityComparer(IORMUtility ormUtility)
    //    {
    //        if (ormUtility == null)
    //        {
    //            throw new ArgumentNullException();
    //        }

    //        _ormUtility = ormUtility;
    //    }

    //    /// <summary>
    //    /// Determines equality for two entities of type TEntity by examining the values of any/all PK properties.
    //    /// If both entities have incomplete PKs, however, object.Equals(entity1, entity2) is returned.
    //    /// </summary>
    //    /// <param name="entity1"></param>
    //    /// <param name="entity2"></param>
    //    /// <returns>false if any PK property values are not equal, otherwise true</returns>
    //    public new bool Equals(object entity1, object entity2)
    //    {
    //        if (entity1 == null || entity2 == null)
    //        {
    //            throw new ArgumentNullException();
    //        }
    //        if (entity1.GetType() != entity2.GetType())
    //        {
    //            throw new ArgumentException("Entity types do not agree.");
    //        }

    //        Type type = entity1.GetType();

    //        foreach (PropertyInfo pkProperty in _ormUtility.GetPrimaryKeys(type))
    //        {
    //            object value1 = pkProperty.GetValue(entity1, null);
    //            object value2 = pkProperty.GetValue(entity2, null);

    //            // If both entities have incomplete PKs, return object.Equals(entity1, entity2).
    //            if ((value1 == null && value2 == null) ||
    //                (object.Equals(value1, Activator.CreateInstance(type)) && object.Equals(value2, Activator.CreateInstance(type))))
    //            {
    //                return object.Equals(entity1, entity2);
    //            }

    //            if (!object.Equals(value1, value2))
    //                return false;
    //        }
    //        return true;
    //    }

    //    /// <summary>
    //    /// Generates a hash code from a combination of the entity type and the values of any/all PK properties
    //    /// by using an XOR (exclusive OR) operation. If entity has an incomplete PK, however, the default hash
    //    /// code from object.GetHashCode() is returned.
    //    /// </summary>
    //    /// <param name="entity"></param>
    //    /// <returns></returns>
    //    public int GetHashCode(object entity)
    //    {
    //        if (entity == null)
    //        {
    //            throw new ArgumentNullException();
    //        }

    //        Type type = entity.GetType();

    //        int hashCode = type.GetHashCode();

    //        foreach (PropertyInfo pkProperty in _ormUtility.GetPrimaryKeys(type))
    //        {
    //            object value = pkProperty.GetValue(entity, null);

    //            // If entity has incomplete PK, return object.GetHashCode().
    //            if (value == null || object.Equals(value, Activator.CreateInstance(type)))
    //                return entity.GetHashCode();

    //            hashCode ^= value.GetHashCode();
    //        }
    //        return hashCode;
    //    }
    //}
}
