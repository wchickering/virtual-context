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
using System.Data.Linq;
using System.Linq.Expressions;
using System.Collections;
using Schrodinger.L2SHelper;
using Schrodinger.EntityTracker;
using System.Reflection;

namespace Schrodinger.EntityFactory
{
    public interface IEntityQuery
    {
        int Count();
        IEntityQuery Detach();
        int GetCount();
        System.Collections.IList GetData();
        System.Collections.IList Load();
        IEntityQuery LoadWith(System.Reflection.PropertyInfo loadProperty);
        object MatchPK(object entity);
        IEntityQuery OrderBy(System.Linq.Expressions.LambdaExpression expression);
        IEntityQuery OrderByDescending(System.Linq.Expressions.LambdaExpression expression);
        IEntityQuery PageNum(int pageNum);
        IEntityQuery PageSize(int pageSize);
        object PK(object pk);
        IEntityQuery Where(System.Linq.Expressions.LambdaExpression predicate);
        IEntityQuery WithDependents();
        IEntityQuery WithFKs();
        IEntityQuery WithFullGraph();
        IEntityQuery WithFullGraphNoFKs();
    }

    /// <summary>
    /// This class represents a query, which may translate into an inividual object, a set of objects,
    /// an object graph, or even a set of object graphs. A connection to the data source is only made
    /// when the Load(DataRequest dataRequest) method of the originating EntityFactory is called.
    /// 
    /// The data source is not contacted until either
    /// PK(), MatchPK(), or Load() is called (or if LoadPending() is called on the EntityFactory which
    /// created this EntityQuery before one of these three methods has been invoked).
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext">DataContext Type to be queried</typeparam>
    public class EntityQuery<TDataContext> : IEntityQuery
        where TDataContext : System.Data.Linq.DataContext, new()
    {
        #region protected_variables

        protected IEntityFactory<TDataContext> _entityFactory;
        protected IORMUtility _ormUtility;
        
        protected bool _isTracking;
        protected DataRequest _dataRequest;

        #endregion protected_variables

        #region constructor

        public EntityQuery(DataRequest dataRequest, IEntityFactory<TDataContext> entityFactory, IORMUtility ormUtility)
        {
            if (dataRequest == null)
            {
                throw new ArgumentNullException("dataRequest");
            }
            if (entityFactory == null)
            {
                throw new ArgumentNullException("entityFactory");
            }
            if (ormUtility == null)
            {
                throw new ArgumentNullException("ormUtility");
            }

            _dataRequest = dataRequest;
            _entityFactory = entityFactory;
            _ormUtility = ormUtility;
        }

        #endregion constructor

        #region public_methods

        //public IEntityFactory<TDataContext> EntityFactory
        //{
        //    get { return _entityFactory; }
        //    set { _entityFactory = value; }
        //}

        virtual public object PK(object pk)
        {
            if (pk == null)
            {
                throw new ArgumentNullException();
            }

            // Generate the 'Where' expression.
            _dataRequest.WhereExpr = WherePKEquals(_dataRequest.RootType, pk);
            
            // Request that EntityFactory 'fills' DataRequest.
            _entityFactory.Load(_dataRequest);

            // Return single entity in entitySet, if it exists.
            foreach (object item in _dataRequest.EntitySet)
            {
                return item;
            }

            // EntitySet empty, so return null.
            return null;
        }

        virtual public object MatchPK(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            // Generate the 'Where' expression.
            _dataRequest.WhereExpr = WherePKMatches(entity);

            // Request that EntityFactory 'fills' DataRequest.
            _entityFactory.Load(_dataRequest);

            // Return single entity in entitySet, if it exists.
            foreach (object item in _dataRequest.EntitySet)
            {
                return item;
            }

            // EntitySet empty, so return null.
            return null;
        }

        virtual public IList Load()
        {
            // Request that EntityFactory 'fills' DataRequest.
            _entityFactory.Load(_dataRequest);

            return _dataRequest.EntitySet;
        }

        virtual public int Count()
        {
            // Request that EntityFactory obtain count for DataRequest.
            _entityFactory.Count(_dataRequest);

            return _dataRequest.Count;
        }

        virtual public IList GetData()
        {
            return _dataRequest.EntitySet;
        }

        virtual public int GetCount()
        {
            return _dataRequest.Count;
        }

        virtual public IEntityQuery Detach()
        {
            _dataRequest.Track = false;
            return this;
        }

        virtual public IEntityQuery Where(LambdaExpression predicate)
        {
            _dataRequest.WhereExpr = predicate;
            return this;
        }

        virtual public IEntityQuery OrderBy(LambdaExpression expression)
        {
            _dataRequest.OrderByExpr = expression;
            return this;
        }

        virtual public IEntityQuery OrderByDescending(LambdaExpression expression)
        {
            _dataRequest.OrderByDescendingExpr = expression;
            return this;
        }

        virtual public IEntityQuery PageSize(int pageSize)
        {
            if (pageSize < 0)
            {
                throw new InvalidOperationException("Page Size must be positive.");
            }
            _dataRequest.PageSize = pageSize;
            return this;
        }

        virtual public IEntityQuery PageNum(int pageNum)
        {
            if (pageNum < 0)
            {
                throw new InvalidOperationException("Page Number must be positive.");
            }
            _dataRequest.PageNum = pageNum;
            return this;
        }

        virtual public IEntityQuery LoadWith(PropertyInfo loadProperty)
        {
            if (loadProperty == null)
            {
                throw new ArgumentNullException();
            }
            if (!_dataRequest.LoadProperties.Contains(loadProperty))
            {
                _dataRequest.LoadProperties.Add(loadProperty);
            }
            return this;
        }

        virtual public IEntityQuery WithFKs()
        {
            foreach (PropertyInfo property in _ormUtility.GetForeignKeyRefs(_dataRequest.RootType))
            {
                if (!_dataRequest.LoadProperties.Contains(property))
                {
                    _dataRequest.LoadProperties.Add(property);
                }
            }
            return this;
        }

        virtual public IEntityQuery WithDependents()
        {
            foreach (PropertyInfo property in _ormUtility.GetDependents(_dataRequest.RootType))
            {
                if (!_dataRequest.LoadProperties.Contains(property))
                {
                    _dataRequest.LoadProperties.Add(property);
                }
            }
            return this;
        }

        virtual public IEntityQuery WithFullGraph()
        {
            foreach (PropertyInfo property in LoadFullGraph(_dataRequest.RootType))
            {
                if (!_dataRequest.LoadProperties.Contains(property))
                {
                    _dataRequest.LoadProperties.Add(property);
                }
            }
            return this;
        }

        virtual public IEntityQuery WithFullGraphNoFKs()
        {
            foreach (PropertyInfo property in LoadFullGraphNoFKs(_dataRequest.RootType))
            {
                if (!_dataRequest.LoadProperties.Contains(property))
                {
                    _dataRequest.LoadProperties.Add(property);
                }
            }
            return this;
        }

        #endregion public_methods

        #region protected_methods

        /// <summary>
        /// Generates a LambdaExpression composed of an equality condition on the primary key of
        /// given entity Type. The expression requires that the primary key valye equal that of object pk.
        /// </summary>
        /// <param name="type">Entity Type</param>
        /// <param name="pk">Entity primary key value</param>
        /// <returns></returns>
        protected LambdaExpression WherePKEquals(Type type, object pk)
        {
            PropertyInfo pkProperty = null;
            try
            {
                pkProperty = _ormUtility.GetPrimaryKeys(type).Single();
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Failed to obtain PK composed of single property.", e);
            }

            // Dynamically construct the lambda expression: (e => e.pkProperty == entity.pkProperty).
            ParameterExpression entityParam = Expression.Parameter(type, type.Name);
            Expression left = Expression.Property(entityParam, pkProperty);
            Expression right = Expression.Constant(pk, pkProperty.PropertyType);
            Expression expression = Expression.Equal(left, right);

            return Expression.Lambda(expression, new ParameterExpression[] { entityParam });
        }

        /// <summary>
        /// Generates a LambdaExpression composed of an equality condition on the primary key of
        /// given entity Type. The expression requires that the primary key equal that of the
        /// privided entity.
        /// </summary>
        /// <param name="entity">Entity with primary key</param>
        /// <returns></returns>
        protected LambdaExpression WherePKMatches(object entity)
        {
            // Dynamically construct the lambda expression: (e => e.pkProperty == entity.pkProperty).
            Expression expression = null;
            ParameterExpression entityParam = Expression.Parameter(entity.GetType(), entity.GetType().Name);
            foreach (PropertyInfo pkProperty in _ormUtility.GetPrimaryKeys(entity.GetType()))
            {
                Expression left = Expression.Property(entityParam, pkProperty);
                Expression right = Expression.Constant(pkProperty.GetValue(entity, null), pkProperty.PropertyType);
                if (expression == null)
                {
                    expression = Expression.Equal(left, right);
                }
                else
                {
                    expression = Expression.And(expression, Expression.Equal(left, right));
                }
            }

            return Expression.Lambda(expression, new ParameterExpression[] { entityParam });
        }

        /// <summary>
        /// Constructs a list of PropertyInfo objects that represents the loading options needed
        /// to retrieve a 'full' object graph with a root entity of the given type. A 'full' object graph
        /// include the root entity, its dependents, its dependents' dependents, and so on. . . (i.e. all
        /// of the descendents of the root entity). In addition to descendents, a 'full' object graph also
        /// includes the foreign key objects of ALL objects in the graph. Note that a 'full' object
        /// graph does NOT include the dependents of entities that are not descendents of the root entity.
        /// This last condition is very important in that it prevents superfluous amounts of data being
        /// retrieved from the database. 
        /// </summary>
        /// <param name="type">Object graph root entity Type</param>
        /// <returns></returns>
        protected List<PropertyInfo> LoadFullGraph(Type type)
        {
            List<PropertyInfo> loadProperties = new List<PropertyInfo>();
            FullyLoadDependents(loadProperties, type, true);
            FullyLoadFKs(loadProperties, type);
            return loadProperties;
        }

        /// <summary>
        /// Same as LoadFullGraph() except that foreign key objects that are not descendents of the root entity
        /// are not included in the graph. Consequently, the object graphs returned by this method are true Tree
        /// Data Structures.
        /// </summary>
        /// <param name="type">Object graph root entity Type</param>
        /// <returns></returns>
        protected List<PropertyInfo> LoadFullGraphNoFKs(Type type)
        {
            List<PropertyInfo> loadProperties = new List<PropertyInfo>();
            FullyLoadDependents(loadProperties, type, false);
            return loadProperties;
        }

        /// <summary>
        /// Obtains a list of PropertyInfo objects that represent the loading options for retrieving a
        /// complete entity tree in the dependent direction (i.e. an object graph consisting of a root object,
        /// dependents, dependents of dependents, and so on. . .). 'Cycles' or loops within the load options
        /// are problematic for O/R mappers. Cycles are avoided by disallowing PropertyInfo objects with a
        /// PropertyType member value that is equal to the DeclaringType member value of another PropertyInfo
        /// object included in the list.
        /// </summary>
        /// <param name="loadProperties">A working list of PropertyInfo objects that represent loading options.</param>
        /// <param name="type">Entity type to generate load options for.</param>
        /// <param name="getFKs">Indicates whether FKs should be loaded.</param>
        protected void FullyLoadDependents(List<PropertyInfo> loadProperties, Type type, bool getFKs)
        {
            if (type == null)
            {
                throw new ArgumentNullException("Must provide Type param.");
            }

            foreach (PropertyInfo dependent in _ormUtility.GetDependents(type))
            {
                Type dependentType = null;
                // One-to-Many?
                if (dependent.PropertyType.IsGenericType && dependent.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                {
                    dependentType = dependent.PropertyType.GetGenericArguments().Single();
                }
                // One-to-One?
                else
                {
                    dependentType = dependent.PropertyType;
                }

                // Avoid cycles.
                if (!loadProperties.Any(p => p.DeclaringType == dependentType) && dependentType != type)
                {
                    loadProperties.Add(dependent);

                    // Recursive Call to load associations of dependent.
                    FullyLoadDependents(loadProperties, dependentType, getFKs);
                    if (getFKs)
                    {
                        FullyLoadFKs(loadProperties, dependentType);
                    }
                }
            }
        }

        /// <summary>
        /// Obtains a list of PropertyInfo objects that represent the loading options for retrieving a
        /// complete entity tree in the foreign key direction (i.e. an object graph consisting of a root
        /// object, its foriegn keys, the foreign keys of the foreign keys, and so on. . .). 'Cycles' or
        /// loops within the load options are problematic for O/R mappers. Cycles are avoided by disallowing
        /// PropertyInfo objects with a PropertyType member value that is equal to the DeclaringType member
        /// value of another PropertyInfo object included in the list.
        /// </summary>
        /// <param name="type">Entity type to generate load options for.</param>
        protected void FullyLoadFKs(List<PropertyInfo> loadProperties, Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException("Must provide Type param.");
            }

            foreach (PropertyInfo foreignKeyRef in _ormUtility.GetForeignKeyRefs(type))
            {
                // Avoid cycles.
                if (!loadProperties.Any(p => p.DeclaringType == foreignKeyRef.PropertyType) && foreignKeyRef.PropertyType != type)
                {
                    loadProperties.Add(foreignKeyRef);

                    // Recursive Call to load associations of foreign key.
                    FullyLoadFKs(loadProperties, foreignKeyRef.PropertyType);
                }
            }
        }

        #endregion protected_methods
    }

    /// <summary>
    /// This is the Generic version that derives from the "typeless" Reflection version above.
    /// This version is merely for convenience.
    /// </summary>
    /// <typeparam name="TDataContext">DataContext Type to be queried</typeparam>
    /// <typeparam name="TEntity">Entity Type to be queried</typeparam>
    public class EntityQuery<TDataContext, TEntity> : EntityQuery<TDataContext>
        where TDataContext : System.Data.Linq.DataContext, new()
        where TEntity : class
    {

        public EntityQuery(DataRequest dataRequest, IEntityFactory<TDataContext> entityFactory, IORMUtility ormUtility)
            : base(dataRequest, entityFactory, ormUtility)
        {
        }

        public new TEntity PK(object pk)
        {
            return base.PK(pk) as TEntity;
        }

        public TEntity MatchPK(TEntity entity)
        {
            return base.MatchPK(entity) as TEntity;
        }

        public new EntitySet<TEntity> Load()
        {
            return base.Load() as EntitySet<TEntity>;
        }

        public new EntitySet<TEntity> GetData()
        {
            return base.GetData() as EntitySet<TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> Detach()
        {
            return base.Detach() as EntityQuery<TDataContext, TEntity>;
        }

        public EntityQuery<TDataContext, TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            return base.Where(predicate) as EntityQuery<TDataContext, TEntity>;
        }

        public EntityQuery<TDataContext, TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> expression)
        {
            return base.OrderBy(expression) as EntityQuery<TDataContext, TEntity>;
        }

        public EntityQuery<TDataContext, TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> expression)
        {
            return base.OrderByDescending(expression) as EntityQuery<TDataContext, TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> PageSize(int pageSize)
        {
            return base.PageSize(pageSize) as EntityQuery<TDataContext, TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> PageNum(int pageNum)
        {
            return base.PageNum(pageNum) as EntityQuery<TDataContext, TEntity>;
        }

        public EntityQuery<TDataContext, TEntity> LoadWith(Expression<Func<TEntity, object>> expression)
        {
            MemberExpression memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                throw new InvalidOperationException("LoadWith requires a MemberExpression.");
            }

            PropertyInfo property = memberExpression.Member as PropertyInfo;
            if (property == null)
            {
                throw new InvalidOperationException("Can only load Properties.");
            }
            if (!_dataRequest.LoadProperties.Contains(property))
            {
                _dataRequest.LoadProperties.Add(property);
            }

            return this;
        }

        public new EntityQuery<TDataContext, TEntity> LoadWith(PropertyInfo loadProperty)
        {
            if (loadProperty == null)
            {
                throw new ArgumentNullException();
            }
            if (!_dataRequest.LoadProperties.Contains(loadProperty))
            {
                _dataRequest.LoadProperties.Add(loadProperty);
            }
            return this;
        }

        public new EntityQuery<TDataContext, TEntity> WithFKs()
        {
            return base.WithFKs() as EntityQuery<TDataContext, TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> WithDependents()
        {
            return base.WithDependents() as EntityQuery<TDataContext, TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> WithFullGraph()
        {
            return base.WithFullGraph() as EntityQuery<TDataContext, TEntity>;
        }

        public new EntityQuery<TDataContext, TEntity> WithFullGraphNoFKs()
        {
            return base.WithFullGraphNoFKs() as EntityQuery<TDataContext, TEntity>;
        }
    }
}
