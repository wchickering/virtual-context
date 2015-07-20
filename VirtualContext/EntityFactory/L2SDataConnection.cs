using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Data.Linq;
using System.Reflection;
using System.IO;

namespace Schrodinger.EntityFactory
{
    /// <summary>
    /// Represents a single connection to a data source.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext">The DataContext type</typeparam>
    public class L2SDataConnection<TDataContext> : Schrodinger.EntityFactory.IDataConnection
        where TDataContext : System.Data.Linq.DataContext, new()
    {
        private TDataContext _dataContext;
        private TextWriter _log;

        private int _queryCount;

        public L2SDataConnection()
            : this(null)
        {
        }

        public L2SDataConnection(TextWriter log)
        {
            _log = log;
            _queryCount = 0;
            _dataContext = new TDataContext();
            _dataContext.ObjectTrackingEnabled = false;
            _dataContext.DeferredLoadingEnabled = false;
            if (_log != null)
            {
                _dataContext.Log = log;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loadProperties"></param>
        public void LoadWith(IEnumerable<PropertyInfo> loadProperties)
        {
            if (loadProperties == null)
            {
                throw new ArgumentNullException();
            }

            List<Type> loadedTypes = new List<Type>();
            DataLoadOptions dataLoadOptions = new DataLoadOptions();
            foreach (PropertyInfo property in loadProperties)
            {
                ParameterExpression entityParam = Expression.Parameter(property.DeclaringType, property.DeclaringType.Name);
                Expression propertyExpression = Expression.Property(entityParam, property);
                dataLoadOptions.LoadWith(Expression.Lambda(propertyExpression, new ParameterExpression[] { entityParam }));
            }
            _dataContext.LoadOptions = dataLoadOptions;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="whereClause"></param>
        /// <param name="orderByClause"></param>
        /// <param name="orderByDescendingClause"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNum"></param>
        /// <returns></returns>
        public IList Get(
            Type rootType,
            LambdaExpression whereExpr,
            LambdaExpression orderByExpr,
            LambdaExpression orderByDescendingExpr,
            int pageSize,
            int pageNum)
        {
            if (rootType == null)
            {
                throw new ArgumentNullException("Must provide rootType param.");
            }

            IQueryable query = _dataContext.GetTable(rootType);
            if (whereExpr != null)
            {
                MethodCallExpression whereCallExpression = Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new Type[] { query.ElementType },
                        query.Expression,
                        whereExpr);
                query = _dataContext.GetTable(rootType).Provider.CreateQuery(whereCallExpression);
            }
            if (orderByExpr != null)
            {
                query = (IQueryable)typeof(Queryable).GetMethods().Single(
                method => method.Name == "OrderBy"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(rootType, orderByExpr.Body.Type)
                .Invoke(null, new object[] { query, orderByExpr });
            }
            if (orderByDescendingExpr != null)
            {
                query = (IQueryable)typeof(Queryable).GetMethods().Single(
                method => method.Name == "OrderByDescending"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 2
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(rootType, orderByDescendingExpr.Body.Type)
                .Invoke(null, new object[] { query, orderByDescendingExpr });
            }
            if (pageNum > 0)
            {
                query = (IQueryable)typeof(Queryable).GetMethods().Single(
                method => method.Name == "Skip"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 1
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(rootType)
                .Invoke(null, new object[] { query, pageSize * (pageNum - 1) });
            }
            if (pageSize > 0)
            {
                query = (IQueryable)typeof(Queryable).GetMethods().Single(
                method => method.Name == "Take"
                        && method.IsGenericMethodDefinition
                        && method.GetGenericArguments().Length == 1
                        && method.GetParameters().Length == 2)
                .MakeGenericMethod(rootType)
                .Invoke(null, new object[] { query, pageSize });
            }
            Type entitySetType = typeof(EntitySet<>).MakeGenericType(new Type[] { rootType });
            IList entitySet = (IList)Activator.CreateInstance(entitySetType);

            if (_log != null)
            {
                _log.WriteLine("---- Executing Query " + ++_queryCount + " ----");
            }

            // Enumerate the results from DB.
            foreach (object item in query)
            {
                entitySet.Add(item);
            }
            
            if (_log != null)
            {
                _log.WriteLine("---------------------------");
            }

            return entitySet;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public int GetCount(Type rootType, LambdaExpression whereExpr)
        {
            if (rootType == null)
            {
                throw new ArgumentNullException("Must provide rootType param.");
            }

            IQueryable query = _dataContext.GetTable(rootType);
            if (whereExpr != null)
            {
                MethodCallExpression whereCallExpression = Expression.Call(
                        typeof(Queryable),
                        "Where",
                        new Type[] { query.ElementType },
                        query.Expression,
                        whereExpr);
                query = _dataContext.GetTable(rootType).Provider.CreateQuery(whereCallExpression);
            }
            if (_log != null)
            {
                _log.WriteLine("---- Executing Query " + ++_queryCount + " ----");
            }
            //return (int)typeof(IQueryable).MakeGenericType(new Type[] { rootType }).GetMethod("Count").Invoke(query, null);
            return (int)query.Provider.Execute(
                Expression.Call(typeof(Queryable),
                "Count",
                new Type[] { query.ElementType },
                query.Expression));
        }
    }
}
