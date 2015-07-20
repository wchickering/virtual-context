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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Linq.Dynamic;
using Schrodinger.L2SHelper;
using Schrodinger.EntityTracker;
using System.Data.Linq;


namespace Schrodinger.EntityFactory
{
    /// <summary>
    /// The name “EntityFactory” is perhaps a misnomer; this class actually produces an object called
    /// an EntityQuery, which contains all the information required to construct a LINQ query that will
    /// retrieve the desired object, object graph, object set, or set of object graphs. Calling an
    /// EntityQuery’s Load() method causes the EntityQuery to request that the EntityFactory fill its
    /// DataRequest. The EntityFactory then instantiates an L2SDataConnection, which in turn instantiates
    /// the DataContext that is ultimately used to retrieve the requested object(s).
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public class EntityFactory<TDataContext> : Schrodinger.EntityFactory.IEntityFactory<TDataContext>
        where TDataContext : System.Data.Linq.DataContext, new()
    {
        #region private_variables

        private IORMUtility _ormUtility;
        private IEntityTracker _entityTracker;

        private TextWriter _log;

        private List<DataRequest> _pendingDataRequests;

        #endregion private_variables

        #region constructor

        public EntityFactory(IORMUtility ormUtility)
            : this(ormUtility, null)
        {
        }

        public EntityFactory(IORMUtility ormUtility, IEntityTracker entityTracker)
        {
            if (ormUtility == null)
            {
                throw new ArgumentNullException("Must provide an ORMUtiliy.");
            }
            _ormUtility = ormUtility;
            _entityTracker = entityTracker;

            _pendingDataRequests = new List<DataRequest>();
        }

        #endregion constructor

        #region public_methods

        public TextWriter Log
        {
            get { return _log; }
            set { _log = value; }
        }
        
        public EntityQuery<TDataContext, TEntity> Get<TEntity>() where TEntity : class
        {
            DataRequest dataRequest = new DataRequest()
            {
                RootType = typeof(TEntity)
            };
            _pendingDataRequests.Add(dataRequest);
            return new EntityQuery<TDataContext, TEntity>(dataRequest, this, _ormUtility);
        }

        /// <summary>
        /// This is a convenience method that simply re-routes to Get<TEntity>().
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EntityQuery<TDataContext> Get(Type type)
        {
            return this.GetType().GetMethods().Single(
                method => method.Name == "Get"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 0)
                    .MakeGenericMethod(new Type[] { type })
                    .Invoke(this, null) as EntityQuery<TDataContext>;
        }

        public EntityQuery<TDataContext, TEntity> GetCount<TEntity>() where TEntity : class
        {
            DataRequest dataRequest = new DataRequest()
            {
                RootType = typeof(TEntity),
                CountOnly = true
            };
            _pendingDataRequests.Add(dataRequest);
            return new EntityQuery<TDataContext, TEntity>(dataRequest, this, _ormUtility);
        }

        /// <summary>
        /// This is a convenience method that simply re-routes to GetCount<TEntity>().
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public EntityQuery<TDataContext> GetCount(Type type)
        {
            return this.GetType().GetMethods().Single(
                method => method.Name == "GetCount"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 0)
                    .MakeGenericMethod(new Type[] { type })
                    .Invoke(this, null) as EntityQuery<TDataContext>;
        }

        /// <summary>
        /// Retrieves the FK objects for a given entity. All FK objects are retrieved  via one instance of DataContext.
        /// None of the FK objects are tracked by the IEntityTracker.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        public void GetFKs<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<PropertyInfo, DataRequest> dataRequestDictionary =
                                              new Dictionary<PropertyInfo,DataRequest>();

            foreach (PropertyInfo foreignKeyRef in _ormUtility.GetForeignKeyRefs(typeof(TEntity)))
            {
                PropertyInfo fkThisKey = _ormUtility.GetFKThisKey(foreignKeyRef);
                PropertyInfo fkOtherKey = _ormUtility.GetFKOtherKey(foreignKeyRef);

                // Only retrieve FK objects when null FK association and FK id is set.
                
                object fkValue = null;
                if (foreignKeyRef.GetValue(entity, null) == null &&
                    (fkValue = fkThisKey.GetValue(entity, null)) != null &&
                    !Equals(fkValue, Activator.CreateInstance(fkValue.GetType())))
                {
                    // Construct 'Where' expression.
                    ParameterExpression parameter = 
                        Expression.Parameter(foreignKeyRef.PropertyType, foreignKeyRef.PropertyType.Name);
                    MemberExpression left = Expression.Property(parameter, fkOtherKey);
                    ConstantExpression right = Expression.Constant(fkValue);
                    BinaryExpression body = Expression.Equal(left, right);
                    LambdaExpression whereExpr = 
                        Expression.Lambda(body, new ParameterExpression[] { parameter });

                    DataRequest dataRequest = new DataRequest()
                    {
                        RootType = foreignKeyRef.PropertyType,
                        WhereExpr = whereExpr,
                        Track = false
                    };

                    _pendingDataRequests.Add(dataRequest);

                    dataRequestDictionary.Add(foreignKeyRef, dataRequest);
                }
            }

            // Load any/all fkEntities via one instance of DataContext.
            LoadPending();

            // Assemble object graph.
            foreach (var kvp in dataRequestDictionary)
            {
                PropertyInfo foreignKeyRef = kvp.Key;
                DataRequest dataRequest = kvp.Value;
 
                foreach(object fkEntity in dataRequest.EntitySet)
                    foreignKeyRef.SetValue(entity, fkEntity, null);
            }
        }

        /// <summary>
        /// This is a convenience method that simply re-routes to
        /// GetFKs<TEntity>(TEntity entity).
        /// </summary>
        /// <param name="entity"></param>
        public void GetFKs(object entity)
        {
            this.GetType().GetMethods().Single(
                method => method.Name == "GetFKs"
                    && method.IsGenericMethodDefinition
                    && method.GetGenericArguments().Length == 1
                    && method.GetParameters().Length == 1)
                    .MakeGenericMethod(new Type[] { entity.GetType() })
                    .Invoke(this, new object[] { entity });
        }

        public void Count(DataRequest dataRequest)
        {
            // Create DataConnection.
            IDataConnection dataConnection = new L2SDataConnection<TDataContext>(_log);

            // Obtain record count.
            GetCount(dataRequest, dataConnection);

            // Remove from list of pending DataRequests (if not already removed).
            if (_pendingDataRequests.Contains(dataRequest))
            {
                _pendingDataRequests.Remove(dataRequest);
            }
        }

        /// <summary>
        /// Creates a DataConnection and 'fills' a DataRequest.
        /// </summary>
        /// <param name="dataRequest">DataRequest to fill</param>
        public void Load(DataRequest dataRequest)
        {
            // Create DataConnection.
            IDataConnection dataConnection = new L2SDataConnection<TDataContext>(_log);
            IDataConnection loadlessDataConnection = null;

            List<PropertyInfo> loadProperties = new List<PropertyInfo>();
            List<PropertyInfo> recursiveProperties = null;
            foreach (PropertyInfo property in dataRequest.LoadProperties)
            {
                // Is property self-recursive dependents?
                if (property.PropertyType.IsGenericType &&
                    property.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>) &&
                    property.PropertyType.GetGenericArguments().Single() == property.DeclaringType)
                {
                    ;// EntityFactory ignores recursive dependents.
                }
                // Is property self-recursive FK?
                else if (property.PropertyType == property.DeclaringType)
                {
                    // Create a separate DataConnection without 'LoadWith' properties.
                    if (loadlessDataConnection == null)
                    {
                        // Dependency injection for IDataConnection.
                        loadlessDataConnection = new L2SDataConnection<TDataContext>(_log);
                    }

                    // Is there already a recursive entry for this DataRequest?
                    if (recursiveProperties != null)
                    {
                        if (!recursiveProperties.Contains(property))
                        {
                            recursiveProperties.Add(property);
                        }
                    }
                    // Add new entry to recursiveProperties.
                    else
                    {
                        recursiveProperties = new List<PropertyInfo>();
                        recursiveProperties.Add(property);
                    }
                }
                // If not there already, add property to loadProperties.
                else
                {
                    if (!loadProperties.Contains(property))
                    {
                        loadProperties.Add(property);
                    }
                }
            }

            // Provide DataConnection with load options.
            dataConnection.LoadWith(loadProperties);

            // Retrieve data, fill request, and initiate tracking.
            FillDataRequest(dataRequest, dataConnection, recursiveProperties, loadlessDataConnection);

            // Remove from list of pending DataRequests (if not already removed).
            if (_pendingDataRequests.Contains(dataRequest))
            {
                _pendingDataRequests.Remove(dataRequest);
            }
        }

        /// <summary>
        /// Creates a single DataConnection and 'fills' all pending DataRequests.
        /// </summary>
        public void LoadPending()
        {
            // Create a single DataConnection.
            IDataConnection dataConnection = new L2SDataConnection<TDataContext>(_log);
            IDataConnection loadlessDataConnection = null;

            // Synthesize all loadProperties into a single list and apply to DataConnection.
            List<PropertyInfo> loadProperties = new List<PropertyInfo>();
            Dictionary<Type, List<PropertyInfo>> recursivePropertyDictionary = new Dictionary<Type, List<PropertyInfo>>();
            foreach (DataRequest dataRequest in _pendingDataRequests)
            {
                foreach (PropertyInfo property in dataRequest.LoadProperties)
                {
                    // Is property self-recursive dependents?
                    if (property.PropertyType.IsGenericType &&
                        property.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>) &&
                        property.PropertyType.GetGenericArguments().Single() == property.DeclaringType)
                    {
                        ;// EntityFactory ignores recursive dependents.
                    }
                    // Is property self-recursive FK?
                    else if (property.PropertyType == property.DeclaringType)
                    {
                        // Create a separate DataConnection without 'LoadWith' properties.
                        if (loadlessDataConnection == null)
                        {
                            // Dependency injection for IDataConnection.
                            loadlessDataConnection = new L2SDataConnection<TDataContext>(_log);
                        }

                        // Is there already a recursive entry for this DataRequest?
                        if (recursivePropertyDictionary.ContainsKey(dataRequest.RootType))
                        {
                            if (!recursivePropertyDictionary[dataRequest.RootType].Contains(property))
                            {
                                recursivePropertyDictionary[dataRequest.RootType].Add(property);
                            }
                        }
                        // Add new entry to recursiveProperties.
                        else
                        {
                            List<PropertyInfo> propertyList = new List<PropertyInfo>();
                            propertyList.Add(property);
                            recursivePropertyDictionary.Add(dataRequest.RootType, propertyList);
                        }
                    }
                    // If not there already, add property to loadProperties.
                    else
                    {
                        if (!loadProperties.Contains(property))
                        {
                            loadProperties.Add(property);
                        }
                    }
                }
            }
            dataConnection.LoadWith(loadProperties);

            // Load all pending DataRequests via single DataConnection.
            foreach (DataRequest dataRequest in _pendingDataRequests)
            {
                List<PropertyInfo> recursiveProperties = null;
                recursivePropertyDictionary.TryGetValue(dataRequest.RootType, out recursiveProperties);
                
                // Retrieve data, fill request, and initiate tracking.
                FillDataRequest(dataRequest, dataConnection, recursiveProperties, loadlessDataConnection);
            }

            // Clear list of pending DataRequests.
            _pendingDataRequests.Clear();
        }

        #endregion public_methods

        #region private_methods

        /// <summary>
        /// Retrieves EntitySet from DataConnection, fills DataRequest, and attaches entity(s) to tracking context.
        /// </summary>
        /// <param name="dataRequest">DataRequest to fill</param>
        /// <param name="dataConnection">DataConnection to utilize</param>
        private void FillDataRequest(DataRequest dataRequest,
                                    IDataConnection dataConnection,
                                    List<PropertyInfo> recursiveProperties,
                                    IDataConnection loadlessDataConnection)
        {
            if (dataRequest.CountOnly)
            {
                GetCount(dataRequest, dataConnection);
            }
            else
            {
                GetData(dataRequest, dataConnection);

                #region self_recursive_load_request
                // Fulfill any self-recursive FK load requests one level deep.
                if (recursiveProperties != null &&
                    recursiveProperties.Count > 0 &&
                    loadlessDataConnection != null)
                {

                    foreach (PropertyInfo recursiveProperty in recursiveProperties)
                    {
                        
                        PropertyInfo thisKey = _ormUtility.GetFKThisKey(recursiveProperty);
                        PropertyInfo otherKey = _ormUtility.GetFKOtherKey(recursiveProperty);
                        
                        // Single object graph returned from DB.
                        if (dataRequest.Count == 1)
                        {
                            object entity = GetFirstItem(dataRequest.EntitySet);
                            object fkValue = thisKey.GetValue(entity, null);

                            if (fkValue != null)
                            {
                                // Dynamically construct 'Where' expression to pick out associated FK Entity.
                                ParameterExpression parameterExpression = Expression.Parameter(recursiveProperty.PropertyType,
                                                                            recursiveProperty.PropertyType.Name);
                                MemberExpression propertyExpression = Expression.Property(parameterExpression, otherKey);
                                ConstantExpression valueExpression = Expression.Constant(fkValue);
                                BinaryExpression equalExpression = Expression.Equal(propertyExpression, valueExpression);
                                LambdaExpression whereExpression = Expression.Lambda(equalExpression,
                                                                      new ParameterExpression[] { parameterExpression });

                                // Retrieve single FK entity from DB.
                                object fkEntity = GetFirstItem(loadlessDataConnection.Get(recursiveProperty.PropertyType,
                                                                                          whereExpression, null, null, 0, 0));

                                // Attach to object graph.
                                recursiveProperty.SetValue(entity, fkEntity, null);
                            }
                        }
                        // Multiple object graphs returned from DB.
                        else if (dataRequest.Count > 1)
                        {

                            // Retrieve ALL FK entities from DB.
                            IList fkEntities = loadlessDataConnection.Get(
                                                     recursiveProperty.PropertyType, null, null, null, 0, 0);

                            // Insert into a Dictionary for easy lookup later.
                            Dictionary<object, object> fkEntityDictionary = new Dictionary<object,object>();
                            foreach (object fkEntity in fkEntities)
                            {
                                fkEntityDictionary.Add(otherKey.GetValue(fkEntity, null), fkEntity);
                            }

                            foreach (object entity in dataRequest.EntitySet)
                            {
                                object fkValue = thisKey.GetValue(entity, null);

                                if (fkValue != null)
                                {
                                    // Lookup FK entity in Dictionary.
                                    object fkEntity = null;
                                    if (fkEntityDictionary.TryGetValue(fkValue, out fkEntity))
                                    {
                                        // Attach to object graph.
                                        recursiveProperty.SetValue(entity, fkEntity, null);
                                    }
                                }
                            }         
                        }  
                    }
                }
                #endregion self_recursive_load_request

                // Attach entity(s) to tracking context.
                if (dataRequest.Track)
                {
                    TrackData(dataRequest.EntitySet);
                }
            }
        }

        private void GetCount(DataRequest dataRequest, IDataConnection dataConnection)
        {
            // Retrieve count from DataConnection.
            int count = dataConnection.GetCount(dataRequest.RootType, dataRequest.WhereExpr);

            // Set Count property of DataRequest.
            dataRequest.Count = count;
        }

        private void GetData(DataRequest dataRequest, IDataConnection dataConnection)
        {
            // Retrieve data from DataConnection.
            IList entitySet = dataConnection.Get(
                dataRequest.RootType,
                dataRequest.WhereExpr,
                dataRequest.OrderByExpr,
                dataRequest.OrderByDescendingExpr,
                dataRequest.PageSize,
                dataRequest.PageNum);

            // Fill DataRequest with data.
            dataRequest.EntitySet = entitySet;
            dataRequest.Count = entitySet.Count;
        }

        private void TrackData(IList entitySet)
        {
            foreach (object entity in entitySet)
            {
                _entityTracker.Track(entity);
                _entityTracker.SetOriginal(entity);
            }
        }

        private object GetFirstItem(IEnumerable enumerable)
        {
            foreach (object item in enumerable)
            {
                return item;
            }
            return null;
        }

        #endregion private_methods
    }
}
