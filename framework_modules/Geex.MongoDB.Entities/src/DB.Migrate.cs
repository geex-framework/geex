using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MongoDB.Entities
{
    public static partial class DB
    {
        /// <summary>
        /// Discover and run migrations from the same assembly as the specified type.
        /// </summary>
        /// <typeparam name="T">A type that is from the same assembly as the migrations you want to run</typeparam>
        public static async Task MigrateAsync<T>() where T : class
        {
            await MigrateAsync(typeof(T)).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes migration classes that implement the IMigration interface in the correct order to transform the database.
        /// <para>TIP: Write classes with names such as: _001_rename_a_field.cs, _002_delete_a_field.cs, etc. and implement IMigration interface on them. Call this method at the startup of the application in order to run the migrations.</para>
        /// </summary>
        public static async Task MigrateAsync()
        {
            await MigrateAsync(null).ConfigureAwait(false);
        }

        public static async Task MigrateAsync(Type targetType)
        {
            IEnumerable<Assembly> assemblies;

            if (targetType == null)
            {
                var excludes = new[]
                {
                    "Microsoft.",
                    "System.",
                    "MongoDB.",
                    "testhost.",
                    "netstandard",
                    "Newtonsoft.",
                    "mscorlib",
                    "NuGet."
                };

                assemblies = AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Where(a =>
                          (!a.IsDynamic && !excludes.Any(n => a.FullName.StartsWith(n))) ||
                          a.FullName.StartsWith("MongoDB.Entities.Tests"));
            }
            else
            {
                assemblies = new[] { targetType.Assembly }.Concat(targetType.Assembly.GetReferencedAssemblies().Select(Assembly.Load));
            }
            var types = assemblies
                .SelectMany(a => a.GetTypes())
                .Where(t => t.BaseType == typeof(DbMigration));

            //if (!types.Any())
            //    throw new InvalidOperationException("Didn't find any classes that implement IMigration interface.");

            await MigrateTargetAsync(types.ToArray()).ConfigureAwait(false);
        }

        public static async Task MigrateTargetAsync(params Type[] migrationTypes)
        {
            using var dbContext = new DbContext();
            await dbContext.MigrateAsync(migrationTypes.Select(x => Activator.CreateInstance(x).As<DbMigration>()));
        }
    }
}
