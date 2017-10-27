using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Linq;

namespace Skyward.Popcorn
{
    public static class EntityFrameworkCore
    {
        private const string DbCountKey = "DbCount";
        private const string DbKey = "Db";


        /// <summary>
        /// Helper function creating a mapping from a source type to a destination type.  Will attempt to auto-load navigation properties as needed.
        /// </summary>
        /// <param name="optionsBuilder"></param>
        /// <param name="defaultIncludes"></param>
        /// <param name="self">todo: describe self parameter on MapEntityFramework</param>
        /// <param name="config"></param>
        /// <typeparam name="TSourceType"></typeparam>
        /// <typeparam name="TDestType"></typeparam>
        /// <typeparam name="TContext"></typeparam>
        /// <returns></returns>
        public static PopcornConfiguration MapEntityFramework<TSourceType, TDestType, TContext>(
            this PopcornConfiguration self,
            DbContextOptionsBuilder<TContext> optionsBuilder, 
            string defaultIncludes = null,
            Action<MappingDefinitionConfiguration<TSourceType, TDestType>> config = null)
            where TContext : DbContext
            where TSourceType : class
            where TDestType : class
        {
            var popcornConfiguration = self.Map<TSourceType, TDestType>(defaultIncludes, (definition) =>
            {
                definition
                    // Before dealing with this object, create a context to use
                    .BeforeExpansion((destinationObject, sourceObject, context) =>
                    {
                        // Do some reference counting here
                        if (!context.ContainsKey(DbKey))
                        {
                            DbContext db = ConstructDbContextWithOptions(optionsBuilder);
                            try
                            {
                                db.Attach(sourceObject);
                                context[DbKey] = db;
                                context[DbCountKey] = 1;
                            }
                            catch { }
                        }
                        else
                        {
                            context[DbCountKey] = (int)context[DbCountKey] + 1;
                        }
                    })
                    // Afterwards clean up our resources
                    .AfterExpansion((destinationObject, sourceObject, context) =>
                    {
                        if (context.ContainsKey(DbKey))
                        {
                            // If the reference count goes to 0, destroy the context
                            var decrementedReferenceCount = (int)context[DbCountKey] - 1;
                            if (decrementedReferenceCount == 0)
                            {
                                (context[DbKey] as IDisposable).Dispose();
                                context.Remove(DbKey);
                                context.Remove(DbCountKey);
                            }
                            else
                            {
                                context[DbCountKey] = decrementedReferenceCount;
                            }
                        }
                    });

                // Now, find all navigation properties on this type and configure each one to load from the database if
                // actually requested for expansion
                using (DbContext db = ConstructDbContextWithOptions(optionsBuilder))
                {
                    foreach (var prop in typeof(TSourceType).GetNavigationReferenceProperties(db))
                    {
                        definition.PrepareProperty(prop.Name, (destinationObject, destinationProperty, sourceObject, context) =>
                        {
                            if (context.ContainsKey(DbKey))
                            {
                                var expandDb = context[DbKey] as TContext;
                                expandDb.Attach(sourceObject as TSourceType);
                                expandDb.Entry(sourceObject as TSourceType).Reference(prop.Name).Load();
                            }
                        });
                    }

                    foreach (var prop in typeof(TSourceType).GetNavigationCollectionProperties(db))
                    {
                        definition.PrepareProperty(prop.Name, (destinationObject, destinationProperty, sourceObject, context) =>
                        {
                            if (context.ContainsKey(DbKey))
                            {
                                var expandDb = context[DbKey] as TContext;
                                expandDb.Attach(sourceObject as TSourceType);
                                expandDb.Entry(sourceObject as TSourceType).Collection(prop.Name).Load();
                            }
                        });
                    }
                }
            });

            return popcornConfiguration.Map(defaultIncludes, config);
        }

        /// <summary>
        /// A helper method to build a DbContext given an options builder.
        /// </summary>
        /// <typeparam name="TContext"></typeparam>
        /// <param name="optionsBuilder"></param>
        /// <returns></returns>
        private static DbContext ConstructDbContextWithOptions<TContext>(DbContextOptionsBuilder<TContext> optionsBuilder) where TContext : DbContext
        {
            var constructor = typeof(TContext).GetConstructor(new Type[] { typeof(DbContextOptions<TContext>) });
            var db = (DbContext)constructor.Invoke(new[] { optionsBuilder.Options });
            return db;
        }


        /// <summary>
        /// Method that uses a DbContext to get a list of 'Navigation' properties that are collections -- that is, properties that represent other entities
        /// rather than strictly data on THIS entity.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetNavigationCollectionProperties(this Type entityType, DbContext context)
        {
            var properties = new List<PropertyInfo>();
            //Get the System.Data.Entity.Core.Metadata.Edm.EntityType
            //associated with the entity.
            var entityElementType = context.Model.FindEntityType(entityType);

            //Iterate each 
            //System.Data.Entity.Core.Metadata.Edm.NavigationProperty
            //in EntityType.NavigationProperties, get the actual property 
            //using the entityType name, and add it to the return set.
            foreach (var navigationProperty in entityElementType.GetNavigations().Where(np => np.IsCollection()))
            {
                properties.Add(entityType.GetProperty(navigationProperty.Name));
            }
            return properties;
        }

        /// <summary>
        /// Method that uses a DbContext to get a list of 'Navigation' properties -- that is, properties that represent other entities
        /// rather than strictly data on THIS entity.
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static List<PropertyInfo> GetNavigationReferenceProperties(this Type entityType, DbContext context)
        {
            var properties = new List<PropertyInfo>();
            //Get the System.Data.Entity.Core.Metadata.Edm.EntityType
            //associated with the entity.
            var entityElementType = context.Model.FindEntityType(entityType);

            //Iterate each 
            //System.Data.Entity.Core.Metadata.Edm.NavigationProperty
            //in EntityType.NavigationProperties, get the actual property 
            //using the entityType name, and add it to the return set.
            foreach (var navigationProperty in entityElementType.GetNavigations().Where(np => !np.IsCollection()))
            {
                properties.Add(entityType.GetProperty(navigationProperty.Name));
            }
            return properties;
        }
    }
}
