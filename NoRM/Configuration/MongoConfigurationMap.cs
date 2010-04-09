﻿using System;
using Norm.BSON;
using System.Collections.Generic;
using System.Reflection;

namespace Norm.Configuration
{
    /// <summary>
    /// Represents configuration mapping types names to database field names
    /// </summary>
    public class MongoConfigurationMap : IMongoConfigurationMap
    {
        private Dictionary<Type, String> _idProperties = new Dictionary<Type, string>();

        /// <summary>
        /// Configures properties for type T
        /// </summary>
        /// <typeparam name="T">Type to configure</typeparam>
        /// <param name="typeConfigurationAction">The type configuration action.</param>
        public void For<T>(Action<ITypeConfiguration<T>> typeConfigurationAction)
        {
            var typeConfiguration = new MongoTypeConfiguration<T>();
            typeConfigurationAction((ITypeConfiguration<T>)typeConfiguration);
        }

        private bool IsIdPropertyForType(Type type, String propertyName)
        {
            bool retval = false;
            if (!_idProperties.ContainsKey(type))
            {
                PropertyInfo idProp = null;
                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.FlattenHierarchy))
                {
                    if (property.GetCustomAttributes(BsonHelper.MongoIdentifierAttribute, true).Length > 0)
                    {
                        idProp = property;
                        break;
                    }
                    if (property.Name.Equals("_id", StringComparison.InvariantCultureIgnoreCase))
                    {
                        idProp = property;
                        break;
                    }
                    if (idProp == null && property.Name.Equals("Id", StringComparison.InvariantCultureIgnoreCase))
                    {
                        idProp = property;
                        break;
                    }
                }
                if (idProp != null)
                {
                    _idProperties[type] = idProp.Name;
                    retval = idProp.Name == propertyName;
                }
            }
            else
            {
                retval = _idProperties[type] == propertyName;
            }
            return retval;
        }

        /// <summary>
        /// Gets the property alias for a type.
        /// </summary>
        /// <remarks>
        /// If it's the ID Property, returns "_id" regardless of additional mapping.
        /// If it's not the ID Property, returns the mapped name if it exists.
        /// Else return the original propertyName.
        /// </remarks>
        /// <param name="type">The type.</param>
        /// <param name="propertyName">Name of the type's property.</param>
        /// <returns>
        /// Type's property alias if configured; otherwise null
        /// </returns>
        public string GetPropertyAlias(Type type, string propertyName)
        {
            var map = MongoTypeConfiguration.PropertyMaps;
            var retval = propertyName;//default to the original.

            if (this.IsIdPropertyForType(type, propertyName))
            {
                retval = "_id";
            }
            else if (map.ContainsKey(type) && map[type].ContainsKey(propertyName))
            {
                retval = map[type][propertyName].Alias;
            }
            return retval;
        }

        /// <summary>
        /// Gets the name of the type's collection.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The get collection name.</returns>
        public string GetCollectionName(Type type)
        {
            var collections = MongoTypeConfiguration.CollectionNames;
            return collections.ContainsKey(type) ? collections[type] : type.Name;
        }

        /// <summary>
        /// Gets the connection string.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The get connection string.</returns>
        public string GetConnectionString(Type type)
        {
            var connections = MongoTypeConfiguration.ConnectionStrings;
            return connections.ContainsKey(type) ? connections[type] : null;
        }

        /// <summary>
        /// Removes the mapping for this type.
        /// </summary>
        /// <remarks>
        /// Added to support Unit testing. Use at your own risk!
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        public void RemoveFor<T>()
        {
            MongoTypeConfiguration.RemoveMappings<T>();
        }

    }
}