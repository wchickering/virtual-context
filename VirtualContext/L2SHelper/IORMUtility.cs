using System;
namespace Schrodinger.L2SHelper
{
    public interface IORMUtility
    {
        System.Collections.Generic.Dictionary<System.Reflection.PropertyInfo, System.Reflection.PropertyInfo> ConstructFKDictionary(Type type);
        bool DeepCompare(object reference, object target);
        object DeserializeEntity(Type type, string serializedEntity);
        object Duplicate(object entity);
        System.Collections.Generic.IEnumerable<Type> GetAllEntityTypes();
        string GetAssemblyQualifiedName(string entityType);
        System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetDBColProperties(Type type);
        System.Reflection.PropertyInfo GetDBColumn(Type type, string propertyName);
        System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetDBGenProperties(Type type);
        System.Reflection.PropertyInfo GetDBGenProperty(Type type, string propertyName);
        System.Reflection.PropertyInfo GetDependent(Type type, string propertyName);
        System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetDependents(Type type);
        Type GetEntityType(string entityType);
        Type GetEntityTypeFromTableName(string tableName);
        System.Reflection.PropertyInfo GetFKAssociation(System.Reflection.PropertyInfo thisKeyProperty);
        System.Reflection.PropertyInfo GetFKOtherKey(System.Reflection.PropertyInfo foreignKeyRef);
        System.Reflection.PropertyInfo GetFKThisKey(System.Reflection.PropertyInfo foreignKeyRef);
        System.Reflection.PropertyInfo GetForeignKeyRef(Type type, string propertyName);
        System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetForeignKeyRefs(Type type);
        System.Reflection.PropertyInfo GetPrimaryKey(Type type, string propertyName);
        System.Collections.Generic.IEnumerable<System.Reflection.PropertyInfo> GetPrimaryKeys(Type type);
        string GetTableNameFromEntityType(Type type);
        bool HasPK(object entity);
        bool IsDBColumnProperty(System.Reflection.PropertyInfo property);
        bool IsDbGenerated(System.Reflection.PropertyInfo property);
        bool IsDependent(System.Reflection.PropertyInfo property);
        bool IsForeignKey(System.Reflection.PropertyInfo property);
        bool IsPrimaryKey(System.Reflection.PropertyInfo property);
        void Modify(object reference, object target);
        bool PKCompare(object entityA, object entityB);
        void PKCopy(object fromEntity, object toEntity);
        string SerializeEntity(object entity);
        bool ShallowCompare(object entityA, object entityB);
        void ShallowCopy(object fromEntity, object toEntity);
        object ShallowCopy(object entity);
        System.Collections.Generic.IEnumerable<object> ToDependentTree(object entity);
        System.Collections.Generic.IEnumerable<object> ToFKTree(object entity);
        System.Collections.Generic.IEnumerable<object> ToFlatGraph(object entity);
    }
}
