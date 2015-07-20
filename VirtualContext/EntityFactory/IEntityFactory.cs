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
