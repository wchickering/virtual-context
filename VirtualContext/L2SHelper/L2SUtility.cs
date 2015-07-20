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
using System.Linq.Expressions;
using System.Reflection;
using System.Data.Linq.Mapping;
using System.Data.Linq;
using System.Collections;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

namespace Schrodinger.L2SHelper
{
    /// <summary>
    /// The class comprises a collection of useful methods for dealing with an arbitrary Linq to SQL
    /// object model.
    /// 
    /// Written by Bill Chickering.
    /// </summary>
    /// <typeparam name="TDataContext"></typeparam>
    public class L2SUtility<TDataContext> : Schrodinger.L2SHelper.IORMUtility
        where TDataContext : System.Data.Linq.DataContext
    {

        #region private_variables

        private string _assemblyName;
        private int _maxObjectsInGraph;

        private Dictionary<string, Type> _tables; // DB table names.

        // Cached PropertyInfo.
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _fkProperties; // FKs.
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _dependentProperties; // Dependents.
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _dbColProperties; // All DB column properties.
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _pkProperties; // PKs.
        private Dictionary<Type, Dictionary<string, PropertyInfo>> _dbGenProperties; // DB Generated Properties.

        #endregion private_variables

        #region constructor

        public L2SUtility()
        {
            _assemblyName = Assembly.GetAssembly(typeof(TDataContext)).GetName().FullName;
            _maxObjectsInGraph = 65536; // HARDCODED PARAMETER
            PopulatePropertyCache();
        }

        private void PopulatePropertyCache()
        {
            _tables = new Dictionary<string, Type>();
            _fkProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            _dependentProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            _dbColProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            _pkProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
            _dbGenProperties = new Dictionary<Type, Dictionary<string, PropertyInfo>>();

            var entityTypes = from t in Assembly.GetAssembly(typeof(TDataContext)).GetTypes()
                              where t.IsClass
                              && t.Namespace == typeof(TDataContext).Namespace
                              && t != typeof(TDataContext)
                              select t;

            foreach (Type type in entityTypes)
            {
                // Verify that this is indeed a L2S Entity, and if so, obtain tableName.
                PropertyInfo tableProperty = typeof(TDataContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.PropertyType.IsGenericType
                        && p.PropertyType.GetGenericTypeDefinition() == typeof(System.Data.Linq.Table<>)
                        && p.PropertyType.GetGenericArguments().Count() == 1
                        && p.PropertyType.GetGenericArguments().Single() == type).SingleOrDefault();
                if (tableProperty != null)
                {
                    _tables.Add(tableProperty.Name, type);

                    _fkProperties.Add(type, new Dictionary<string, PropertyInfo>());
                    _dependentProperties.Add(type, new Dictionary<string, PropertyInfo>());
                    _dbColProperties.Add(type, new Dictionary<string, PropertyInfo>());
                    _pkProperties.Add(type, new Dictionary<string, PropertyInfo>());
                    _dbGenProperties.Add(type, new Dictionary<string, PropertyInfo>());

                    foreach (PropertyInfo property in type.GetProperties())
                    {
                        AssociationAttribute assocAttribute;
                        ColumnAttribute colAttribute;

                        // Associations.
                        if ((assocAttribute = (AssociationAttribute)Attribute.GetCustomAttribute(property, typeof(AssociationAttribute), false)) != null)
                        {
                            // FKs.
                            if (assocAttribute.IsForeignKey)
                            {
                                _fkProperties[type].Add(property.Name, property);
                            }
                            // Dependents.
                            else
                            {
                                _dependentProperties[type].Add(property.Name, property);
                            }
                        }
                        else if ((colAttribute = Attribute.GetCustomAttribute(property, typeof(ColumnAttribute), false) as ColumnAttribute) != null)
                        {
                            // All DB Column Properties.
                            _dbColProperties[type].Add(property.Name, property);

                            // PKs.
                            if (colAttribute.IsPrimaryKey)
                            {
                                _pkProperties[type].Add(property.Name, property);
                                continue;
                            }

                            // DB Generated Properties.
                            if (colAttribute.IsDbGenerated)
                            {
                                _dbGenProperties[type].Add(property.Name, property);
                                continue;
                            }
                        }
                    }
                }
            }
        }

        #endregion constructor

        #region public_methods

        public Type GetEntityType(string entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException();
            }

            return Type.GetType(GetAssemblyQualifiedName(entityType));
        }

        public Type GetEntityTypeFromTableName(string tableName)
        {
            if (tableName == null)
            {
                throw new ArgumentNullException();
            }

            Type type = null;
            if (_tables.TryGetValue(tableName, out type))
            {
                return type;
            }
            return null;
        }

        public string GetTableNameFromEntityType(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            return _tables.Where(kvp => kvp.Value == type).SingleOrDefault().Key;
        }

        public string GetAssemblyQualifiedName(string entityType)
        {
            if (entityType == null)
            {
                throw new ArgumentNullException();
            }

            return typeof(TDataContext).Namespace + "." + entityType + ", " + _assemblyName;
        }

        public IEnumerable<Type> GetAllEntityTypes()
        {
            return _dbColProperties.Keys;
        }

        public IEnumerable<PropertyInfo> GetForeignKeyRefs(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> fkDictionary = null;
            if (!_fkProperties.TryGetValue(type, out fkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return fkDictionary.Values;
        }

        public IEnumerable<PropertyInfo> GetDependents(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dependentDictionary = null;
            if (!_dependentProperties.TryGetValue(type, out dependentDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dependentDictionary.Values;
        }

        public IEnumerable<PropertyInfo> GetDBColProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dbColDictionary = null;
            if (!_dbColProperties.TryGetValue(type, out dbColDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dbColDictionary.Values;
        }

        public IEnumerable<PropertyInfo> GetPrimaryKeys(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> pkDictionary = null;
            if (!_pkProperties.TryGetValue(type, out pkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return pkDictionary.Values;
        }

        public IEnumerable<PropertyInfo> GetDBGenProperties(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dbGenDictionary = null;
            if (!_dbGenProperties.TryGetValue(type, out dbGenDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dbGenDictionary.Values;
        }

        public bool HasPK(object entity)
        {
            foreach (PropertyInfo pkProperty in GetPrimaryKeys(entity.GetType()))
            {
                if (Equals(pkProperty.GetValue(entity, null), Default(pkProperty.PropertyType)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool PKCompare(object entityA, object entityB)
        {
            if (entityA.GetType() != entityB.GetType())
            {
                throw new ArgumentException("Entity Types do not agree.");
            }
            foreach (PropertyInfo pkProperty in GetPrimaryKeys(entityA.GetType()))
            {
                if (!Equals(pkProperty.GetValue(entityA, null), pkProperty.GetValue(entityB, null)))
                {
                    return false;
                }
            }
            return true;
        }

        public bool ShallowCompare(object entityA, object entityB)
        {
            if (entityA.GetType() != entityB.GetType())
            {
                throw new ArgumentException("Entity Types do not agree.");
            }

            foreach (PropertyInfo property in GetDBColProperties(entityA.GetType()))
            {
                if (!Equals(property.GetValue(entityA, null), property.GetValue(entityB, null)))
                {
                    return false;
                }
            }
            return true;
        }

        public object ShallowCopy(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Unkown entity type.");
            }

            object copy = Activator.CreateInstance(entity.GetType());
            foreach (PropertyInfo property in GetDBColProperties(entity.GetType()))
            {
                //property.SetValue(copy, property.GetValue(entity, null), null);
                CopyProperty(property, entity, copy);
            }
            return copy;
        }

        public void ShallowCopy(object fromEntity, object toEntity)
        {
            if (fromEntity == null || toEntity == null)
            {
                throw new ArgumentNullException();
            }
            if (fromEntity.GetType() != toEntity.GetType())
            {
                throw new ArgumentException("Entity Types do not agree.");
            }
            if (!GetAllEntityTypes().Contains(fromEntity.GetType()))
            {
                throw new InvalidOperationException("Unkown entity type.");
            }

            foreach (PropertyInfo property in GetDBColProperties(fromEntity.GetType()))
            {
                //property.SetValue(toEntity, property.GetValue(fromEntity, null), null);
                CopyProperty(property, fromEntity, toEntity);
            }
        }

        public void PKCopy(object fromEntity, object toEntity)
        {
            if (fromEntity == null || toEntity == null)
            {
                throw new ArgumentNullException();
            }
            if (fromEntity.GetType() != toEntity.GetType())
            {
                throw new ArgumentException("Param entity Types do not agree.");
            }
            if (!GetAllEntityTypes().Contains(fromEntity.GetType()))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            foreach (PropertyInfo pkProperty in GetPrimaryKeys(fromEntity.GetType()))
            {
                pkProperty.SetValue(toEntity, pkProperty.GetValue(fromEntity, null), null);
            }
        }

        public Dictionary<PropertyInfo, PropertyInfo> ConstructFKDictionary(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(type))
            {
                throw new InvalidOperationException("Unkown entity type.");
            }

            Dictionary<PropertyInfo, PropertyInfo> foreignKeyDictionary = new Dictionary<PropertyInfo, PropertyInfo>();
            foreach (PropertyInfo foreignKeyRef in GetForeignKeyRefs(type))
            {
                foreignKeyDictionary.Add(GetFKThisKey(foreignKeyRef), foreignKeyRef);
            }
            return foreignKeyDictionary;
        }

        // Returns null if this is NOT a FK Association.
        public PropertyInfo GetFKThisKey(PropertyInfo foreignKeyRef)
        {
            if (foreignKeyRef == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(foreignKeyRef.DeclaringType))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            if (!GetForeignKeyRefs(foreignKeyRef.DeclaringType).Contains(foreignKeyRef))
            {
                return null;
            }

            AssociationAttribute associationAttribute = ((AssociationAttribute[])foreignKeyRef.GetCustomAttributes(typeof(AssociationAttribute), false)).Single();
            return foreignKeyRef.DeclaringType.GetProperty(associationAttribute.ThisKey);
        }

        // Returns null if this is NOT a FK Association.
        public PropertyInfo GetFKOtherKey(PropertyInfo foreignKeyRef)
        {
            if (foreignKeyRef == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(foreignKeyRef.DeclaringType))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            if (!GetForeignKeyRefs(foreignKeyRef.DeclaringType).Contains(foreignKeyRef))
            {
                return null;
            }

            AssociationAttribute associationAttribute = ((AssociationAttribute[])foreignKeyRef.GetCustomAttributes(typeof(AssociationAttribute), false)).Single();
            return foreignKeyRef.PropertyType.GetProperty(associationAttribute.OtherKey);
        }

        // Returns null if this is NOT a FK.
        public PropertyInfo GetFKAssociation(PropertyInfo thisKeyProperty)
        {
            if (thisKeyProperty == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(thisKeyProperty.DeclaringType))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            if (!GetDBColProperties(thisKeyProperty.DeclaringType).Contains(thisKeyProperty))
            {
                return null;
            }
            foreach (PropertyInfo foreignKeyRef in GetForeignKeyRefs(thisKeyProperty.DeclaringType))
            {
                if (thisKeyProperty == GetFKThisKey(foreignKeyRef))
                {
                    return foreignKeyRef;
                }
            }
            return null;
        }

        public bool IsForeignKey(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException();
            }
            Dictionary<string, PropertyInfo> fkDictionary = null;
            if (!_fkProperties.TryGetValue(property.DeclaringType, out fkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return fkDictionary.ContainsValue(property);
        }

        public bool IsDependent(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException();
            }
            Dictionary<string, PropertyInfo> dependentDictionary = null;
            if (!_dependentProperties.TryGetValue(property.DeclaringType, out dependentDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dependentDictionary.ContainsValue(property);
        }

        public bool IsDBColumnProperty(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException();
            }
            Dictionary<string, PropertyInfo> dbColDictionary = null;
            if (!_dbColProperties.TryGetValue(property.DeclaringType, out dbColDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dbColDictionary.ContainsValue(property);
        }

        public bool IsPrimaryKey(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> pkDictionary = null;
            if (!_pkProperties.TryGetValue(property.DeclaringType, out pkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return pkDictionary.ContainsValue(property);
        }

        public bool IsDbGenerated(PropertyInfo property)
        {
            if (property == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dbGenDictionary = null;
            if (!_dbGenProperties.TryGetValue(property.DeclaringType, out dbGenDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            return dbGenDictionary.ContainsValue(property);
        }

        public PropertyInfo GetForeignKeyRef(Type type, string propertyName)
        {
            if (type == null || propertyName == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> fkDictionary = null;
            if (!_fkProperties.TryGetValue(type, out fkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            PropertyInfo foreignKeyRef = null;
            fkDictionary.TryGetValue(propertyName, out foreignKeyRef);
            return foreignKeyRef;
        }

        public PropertyInfo GetDependent(Type type, string propertyName)
        {
            if (type == null || propertyName == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dependentDictionary = null;
            if (!_dependentProperties.TryGetValue(type, out dependentDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            PropertyInfo dependent = null;
            dependentDictionary.TryGetValue(propertyName, out dependent);
            return dependent;
        }

        public PropertyInfo GetDBColumn(Type type, string propertyName)
        {
            if (type == null || propertyName == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dbColDictionary = null;
            if (!_dbColProperties.TryGetValue(type, out dbColDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            PropertyInfo dbCol = null;
            dbColDictionary.TryGetValue(propertyName, out dbCol);
            return dbCol;
        }

        public PropertyInfo GetPrimaryKey(Type type, string propertyName)
        {
            if (type == null || propertyName == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> pkDictionary = null;
            if (!_pkProperties.TryGetValue(type, out pkDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            PropertyInfo pkProperty = null;
            pkDictionary.TryGetValue(propertyName, out pkProperty);
            return pkProperty;
        }

        public PropertyInfo GetDBGenProperty(Type type, string propertyName)
        {
            if (type == null || propertyName == null)
            {
                throw new ArgumentNullException();
            }

            Dictionary<string, PropertyInfo> dbGenDictionary = null;
            if (!_dbGenProperties.TryGetValue(type, out dbGenDictionary))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            PropertyInfo dbGenProperty = null;
            dbGenDictionary.TryGetValue(propertyName, out dbGenProperty);
            return dbGenProperty;
        }

        /// <summary>
        /// Uses DataContractSerializer to serialize an object graph.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public string SerializeEntity(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            DataContractSerializer dcs = new DataContractSerializer(entity.GetType(), GetAllEntityTypes(), _maxObjectsInGraph, false, true, null);
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriter xmlWriter = XmlWriter.Create(stringBuilder);
            dcs.WriteObject(xmlWriter, entity);
            xmlWriter.Close();
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Uses DataContractSerializer to deserialize an object graph.
        /// </summary>
        /// <param name="type">Type of Root entity</param>
        /// <param name="serializedEntity">Serialized object graph</param>
        /// <returns></returns>
        public object DeserializeEntity(Type type, string serializedEntity)
        {
            if (type == null || serializedEntity == null)
            {
                throw new ArgumentNullException();
            }

            DataContractSerializer dcs = new DataContractSerializer(type, GetAllEntityTypes(), _maxObjectsInGraph, false, true, null);
            StringReader stringReader = new StringReader(serializedEntity);
            XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
            object entity = dcs.ReadObject(xmlTextReader);
            xmlTextReader.Close();
            return entity;
        }

        /// <summary>
        /// Uses the DataContractSerializer, via the SerializeEntity() and DeserializeEntity() methods, to
        /// duplicate a complete object graph.
        /// </summary>
        /// <param name="entity">Root entity of graph</param>
        /// <returns>Duplicated graph</returns>
        public object Duplicate(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            string serializedObjectGraph = SerializeEntity(entity);
            return DeserializeEntity(entity.GetType(), serializedObjectGraph);
        }

        public bool DeepCompare(object reference, object target)
        {
            if (reference == null || target == null)
            {
                throw new ArgumentNullException();
            }
            if (reference.GetType() != target.GetType())
            {
                throw new ArgumentException("Entity types do not agree.");
            }

            Type type = reference.GetType();
            if (!GetAllEntityTypes().Contains(type))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            if (!ShallowCompare(reference, target))
            {
                return false;
            }

            foreach (PropertyInfo dependent in GetDependents(type))
            {
                // Collection of dependents.
                if (dependent.PropertyType.IsGenericType && dependent.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                {
                    foreach (object targetDependent in dependent.GetValue(target, null) as IEnumerable)
                    {
                        bool foundEntity = false;
                        foreach (object referenceDependent in dependent.GetValue(reference, null) as IEnumerable)
                        {
                            // Matching dependent.
                            if (PKCompare(referenceDependent, targetDependent))
                            {
                                foundEntity = true;
                                // Recursive Call.
                                if (!DeepCompare(referenceDependent, targetDependent))
                                {
                                    return false;
                                }
                                break;
                            }
                        }
                        // Missing dependent.
                        if (!foundEntity)
                        {
                            return false;
                        }
                    }
                    foreach (object referenceDependent in dependent.GetValue(reference, null) as IEnumerable)
                    {
                        bool foundEntity = false;
                        foreach (object targetDependent in dependent.GetValue(target, null) as IEnumerable)
                        {
                            if (PKCompare(targetDependent, referenceDependent))
                            {
                                foundEntity = true;
                                break;
                            }
                        }
                        // New dependent.
                        if (!foundEntity)
                        {
                            return false;
                        }
                    }
                }
                // 1:1 Relation.
                else
                {
                    object targetDependent = null;
                    object referenceDependent = null;
                    if ((targetDependent = dependent.GetValue(target, null)) != null)
                    {
                        if ((referenceDependent = dependent.GetValue(reference, null)) != null)
                        {
                            // Recursive call.
                            if (!DeepCompare(referenceDependent, targetDependent))
                            {
                                return false;
                            }
                        }
                        // Missing dependent.
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // New dependent.
                        if ((referenceDependent = dependent.GetValue(reference, null)) != null)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Modifies target object graph to be identical to reference object graph. All modifications
        /// are done by value; that is, assuming there are no object references in common before Modify()
        /// is called, there are no object references in common between the reference and target graphs
        /// following the call.
        /// </summary>
        /// <param name="reference">Root entity of graph referenced by modify logic</param>
        /// <param name="target">Root entity of graph to be modified</param>
        public void Modify(object reference, object target)
        {
            if (reference == null || target == null)
            {
                throw new ArgumentNullException();
            }
            if (reference.GetType() != target.GetType())
            {
                throw new ArgumentException("Entity types do not agree.");
            }

            Type type = reference.GetType();
            if (!GetAllEntityTypes().Contains(type))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }
            if (!PKCompare(reference, target))
            {
                throw new InvalidOperationException("Primary Keys of entities are not equal.");
            }

            // Copy DB columns of root entity.
            ShallowCopy(reference, target);

            foreach (PropertyInfo dependent in GetDependents(type))
            {
                // Collection of dependents.
                if (dependent.PropertyType.IsGenericType && dependent.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                {
                    // Must first read all target's dependents into memory so that we can delete any as necessary.
                    var targetDependentsList = new List<object>();
                    foreach (object targetDependent in dependent.GetValue(target, null) as IEnumerable)
                    {
                        targetDependentsList.Add(targetDependent);
                    }
                    foreach (object targetDependent in targetDependentsList)
                    {
                        bool foundEntity = false;
                        foreach (object referenceDependent in dependent.GetValue(reference, null) as IEnumerable)
                        {
                            // Modify found dependent.
                            if (PKCompare(referenceDependent, targetDependent))
                            {
                                foundEntity = true;
                                Modify(referenceDependent, targetDependent);
                                break;
                            }
                        }
                        // Delete missing dependent.
                        if (!foundEntity)
                        {
                            dependent.PropertyType.GetMethod("Remove").Invoke(dependent.GetValue(target, null), new object[] { targetDependent });
                        }
                    }
                    foreach (object referenceDependent in dependent.GetValue(reference, null) as IEnumerable)
                    {
                        bool foundEntity = false;
                        foreach (object targetDependent in dependent.GetValue(target, null) as IEnumerable)
                        {
                            if (PKCompare(targetDependent, referenceDependent))
                            {
                                foundEntity = true;
                                break;
                            }
                        }
                        // Insert new dependent.
                        if (!foundEntity)
                        {
                            object newDependent = Duplicate(referenceDependent);
                            dependent.PropertyType.GetMethod("Add").Invoke(dependent.GetValue(target, null), new object[] { newDependent });
                        }
                    }
                }
                // 1:1 Relation.
                // The following logic is untested. --BC 11/7/10
                else
                {
                    object targetDependent = null;
                    object referenceDependent = null;
                    if ((targetDependent = dependent.GetValue(target, null)) != null)
                    {
                        if ((referenceDependent = dependent.GetValue(reference, null)) != null)
                        {
                            // Modify found dependent.
                            if (PKCompare(referenceDependent, targetDependent))
                            {
                                Modify(referenceDependent, targetDependent);
                            }
                        }
                        // Delete missing dependent.
                        else
                        {
                            //dependent.PropertyType.GetMethod("Remove").Invoke(target, new object[] { targetDependent });
                            dependent.SetValue(target, null, null);
                        }
                    }
                    else
                    {
                        // Insert new dependent.
                        if ((referenceDependent = dependent.GetValue(reference, null)) != null)
                        {
                            object newEntity = Duplicate(referenceDependent);
                            //dependent.PropertyType.GetMethod("Add").Invoke(target, new object[] { newEntity });
                            dependent.SetValue(target, newEntity, null);
                        }
                    }
                }
            }
        }

        public IEnumerable<object> ToDependentTree(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            return new EntityTree(entity, GetDependents);
        }

        public IEnumerable<object> ToFKTree(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            return new EntityTree(entity, GetForeignKeyRefs);
        }

        public IEnumerable<object> ToFlatGraph(object entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException();
            }
            if (!GetAllEntityTypes().Contains(entity.GetType()))
            {
                throw new InvalidOperationException("Unknown entity type.");
            }

            return new EntityGraph(entity, null, GetDependents, GetForeignKeyRefs);
        }

        #endregion public_methods

        #region private_methods

        // A Reflection style Default() method analogous to the default() keyword.
        private object Default(Type type)
        {
            if (!type.IsValueType)
            {
                return null;
            }
            else
            {
                return Activator.CreateInstance(type);
            }
        }

        private void CopyProperty(PropertyInfo property, object fromEntity, object toEntity)
        {
            PropertyInfo foreignKeyRef;
            object fkEntity;

            // L2S only allows manipulation of foreign key IDs when the foreign key entities have NOT been loaded.
            if ((foreignKeyRef = GetFKAssociation(property)) != null && (fkEntity = foreignKeyRef.GetValue(toEntity, null)) != null)
            {
                foreignKeyRef.SetValue(toEntity, foreignKeyRef.GetValue(fromEntity, null), null);
            }
            else
            {
                property.SetValue(toEntity, property.GetValue(fromEntity, null), null);
            }
        }

        #endregion private_methods

        #region private_classes

        /// <summary>
        /// A delegate so we can pass a reference to the GetDependents() or GetForeignKeyRefs() method
        /// into the EntityTree class.
        /// </summary>
        /// <param name="type">Root entity type</param>
        /// <returns></returns>
        private delegate IEnumerable<PropertyInfo> GetAssociationsDelegate(Type type);

        /// <summary>
        /// Implementation of an Enumerator for an Object Tree. A Tree, in this context, consists of the root entity
        /// and either all descendents (dependents, dependents of dependents, etc.) OR all antecedents (foreign keys,
        /// foreign keys of foriegn keys, etc.).
        /// </summary>
        private class EntityTree : IEnumerable<object>
        {
            private GetAssociationsDelegate _GetAssociations;
            private object _entity;
            
            /// <summary>
            /// EntityTree Constructor.
            /// </summary>
            /// <param name="entity">Entity at root of Tree</param>
            /// <param name="del">Reference to either GetDependents() or GetForeignKeyRefs() method</param>
            public EntityTree(object entity, GetAssociationsDelegate getAssociations)
            {
                _entity = entity;
                _GetAssociations = getAssociations;
            }

            /// <summary>
            /// Implementation of GetEnumerator() method.
            /// </summary>
            /// <returns>Enumeration of entities.</returns>
            public IEnumerator<object> GetEnumerator()
            {
                // Return Tree root.
                yield return _entity;

                // Return Associations.
                foreach (PropertyInfo associationProperty in _GetAssociations(_entity.GetType()))
                {
                    // Collection of associations.
                    if (associationProperty.PropertyType.IsGenericType && associationProperty.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                    {
                        foreach (object associatedEntity in associationProperty.GetValue(_entity, null) as IEnumerable)
                        {
                            foreach (object subEntity in new EntityTree(associatedEntity, _GetAssociations))
                            {
                                yield return subEntity;
                            }
                        }
                    }
                    // 1:1 Relation.
                    else
                    {
                        object associatedEntity = null;
                        if ((associatedEntity = associationProperty.GetValue(_entity, null)) != null)
                        {
                            foreach (object subEntity in new EntityTree(associatedEntity, _GetAssociations))
                            {
                                yield return subEntity;
                            }
                        }
                    }
                }
            }

            // Implement the GetEnumerator type.
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        /// <summary>
        /// Implementation of an Enumerator for a complete object graph.
        /// WARNING: This Enumerator can produce a stack overflow if unidirectional loops exist
        /// in object model/entity relationship diagram. 
        /// </summary>
        private class EntityGraph : IEnumerable<object>
        {
            private GetAssociationsDelegate _GetDependents;
            private GetAssociationsDelegate _GetForeignKeyRefs;
            private object _entity;
            private object _declaringEntity;

            /// <summary>
            /// EntityGraph Constructor.
            /// </summary>
            /// <param name="entity">Entity somewhere in graph</param>
            /// <param name="declaringEntity">Referencing entity; used to avoid cycles</param>
            /// <param name="getDependents">Reference to GetDependents() method</param>
            /// <param name="getForeignKeyRefs">Reference to GetForeignKeyRefs() method</param>
            public EntityGraph(
                object entity,
                object declaringEntity,
                GetAssociationsDelegate getDependents,
                GetAssociationsDelegate getForeignKeyRefs)
            {
                _entity = entity;
                _declaringEntity = declaringEntity;
                _GetDependents = getDependents;
                _GetForeignKeyRefs = getForeignKeyRefs;
            }

            /// <summary>
            /// Implementation of GetEnumerator() method.
            /// </summary>
            /// <returns>Enumeration of entities.</returns>
            public IEnumerator<object> GetEnumerator()
            {
                // Return Tree root.
                yield return _entity;

                // Traverse Dependent Associations.
                foreach (PropertyInfo dependent in _GetDependents(_entity.GetType()))
                {
                    // Collection of dependents.
                    if (dependent.PropertyType.IsGenericType && dependent.PropertyType.GetGenericTypeDefinition() == typeof(EntitySet<>))
                    {
                        foreach (object dependentEntity in dependent.GetValue(_entity, null) as IEnumerable)
                        {
                            // Avoid Cycles.
                            if (!object.ReferenceEquals(dependentEntity, _declaringEntity))
                            {
                                foreach (object subEntity in new EntityGraph(
                                    dependentEntity,
                                    _entity,
                                    _GetDependents,
                                    _GetForeignKeyRefs))
                                {
                                    yield return subEntity;
                                }
                            }
                        }
                    }
                    // 1:1 Relation.
                    else
                    {
                        object dependentEntity = null;
                        if ((dependentEntity = dependent.GetValue(_entity, null)) != null)
                        {
                            // Avoid Cycles.
                            if (!object.ReferenceEquals(dependentEntity, _declaringEntity))
                            {
                                foreach (object subEntity in new EntityGraph(
                                    dependentEntity,
                                    _entity,
                                    _GetDependents,
                                    _GetForeignKeyRefs))
                                {
                                    yield return subEntity;
                                }
                            }
                        }
                    }
                }

                // Traverse FK Associations.
                foreach (PropertyInfo foreignKeyRef in _GetForeignKeyRefs(_entity.GetType()))
                {

                        object fkEntity = null;
                        if ((fkEntity = foreignKeyRef.GetValue(_entity, null)) != null)
                        {
                            if (!object.ReferenceEquals(fkEntity, _declaringEntity))
                            {
                                foreach (object subEntity in new EntityGraph(
                                    fkEntity,
                                    _entity,
                                    _GetDependents,
                                    _GetForeignKeyRefs))
                                {
                                    yield return subEntity;
                                }
                            }
                    }
                }
            }

            // Implement the GetEnumerator type.
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }
        }

        #endregion private_classes
    }
}
