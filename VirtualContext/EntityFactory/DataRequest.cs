using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Linq.Expressions;

namespace Schrodinger.EntityFactory
{
    /// <summary>
    /// Represents a single request for data.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    public class DataRequest
    {
        private int _count;
        private IList _entitySet;
        private Type _type;
        List<PropertyInfo> _loadProperties;
        LambdaExpression _whereExpr;
        LambdaExpression _orderByExpr;
        LambdaExpression _orderByDescendingExpr;
        int _pageSize;
        int _pageNum;
        bool _countOnly;
        bool _track;

        public DataRequest()
        {
            _count = 0;
            _loadProperties = new List<PropertyInfo>();
            _pageSize = 0;
            _pageNum = 0;
            _countOnly = false;
            _track = true;
        }

        public int Count
        {
            get { return _count; }
            set { _count = value; }
        }

        public IList EntitySet
        {
            get { return _entitySet; }
            set { _entitySet = value; }
        }

        public Type RootType
        {
            get { return _type; }
            set { _type = value; }
        }

        public List<PropertyInfo> LoadProperties
        {
            get { return _loadProperties; }
            set { _loadProperties = value; }
        }

        public int PageSize
        {
            get { return _pageSize; }
            set { _pageSize = value; }
        }

        public int PageNum
        {
            get { return _pageNum; }
            set { _pageNum = value; }
        }

        public bool CountOnly
        {
            get { return _countOnly; }
            set { _countOnly = value; }
        }

        public bool Track
        {
            get { return _track; }
            set { _track = value; }
        }

        public LambdaExpression WhereExpr
        {
            get { return _whereExpr; }
            set { _whereExpr = value; }
        }

        public LambdaExpression OrderByExpr
        {
            get { return _orderByExpr; }
            set { _orderByExpr = value; }
        }

        public LambdaExpression OrderByDescendingExpr
        {
            get { return _orderByDescendingExpr; }
            set { _orderByDescendingExpr = value; }
        }
    }
}
