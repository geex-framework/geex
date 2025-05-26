using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Geex.Gql.Types;
using Geex.Tests.TestEntities;

using HotChocolate;
using HotChocolate.Types;

using Microsoft.Extensions.DependencyInjection;

namespace Geex.Tests.SchemaTests
{
    public class CoreInterceptorsTests : IClassFixture<GeexWebApplicationFactory>
    {
        public class TestMutation : MutationExtension<TestMutation>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<TestMutation> descriptor)
            {
                base.Configure(descriptor);
            }

            public TestEntity DirectQuery(string arg1) => throw new NotImplementedException();
            public Lazy<TestEntity> LazyQuery(string arg1) => throw new NotImplementedException();
            public IQueryable<TestEntity> IQueryableQuery(string arg1) => throw new NotImplementedException();
        }

        private readonly GeexWebApplicationFactory _factory;

        public CoreInterceptorsTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SpecialFieldsShouldBeIgnored()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();
            // todo
        }
    }
}
