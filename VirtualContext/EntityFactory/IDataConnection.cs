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
