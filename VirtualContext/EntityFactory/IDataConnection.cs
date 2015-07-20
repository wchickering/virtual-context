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
namespace Schrodinger.EntityFactory
{
    public interface IDataConnection
    {
        System.Collections.IList Get(Type rootType, System.Linq.Expressions.LambdaExpression whereExpr, System.Linq.Expressions.LambdaExpression orderByExpr, System.Linq.Expressions.LambdaExpression orderByDescendingExpr, int pageSize, int pageNum);
        int GetCount(Type rootType, System.Linq.Expressions.LambdaExpression whereExpr);
        void LoadWith(System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> loadProperties);
    }
}
