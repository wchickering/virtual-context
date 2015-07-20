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
