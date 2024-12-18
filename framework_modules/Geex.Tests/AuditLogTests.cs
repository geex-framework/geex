﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Geex.Common;
using Geex.Common.Abstraction.Approval;
using Geex.Common.Abstraction.Gql.Types;
using Geex.Common.AuditLogs;
using Geex.Tests.TestEntities;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Geex.Tests
{
    public class AuditLogTests : IClassFixture<GeexWebApplicationFactory>
    {
        public class AuditLogMutation : MutationExtension<AuditLogMutation>, IHasApproveMutation<ApproveEntity>
        {
            /// <inheritdoc />
            protected override void Configure(IObjectTypeDescriptor<AuditLogMutation> descriptor)
            {
                descriptor.AuditFieldsImplicitly();
                base.Configure(descriptor);
            }

            public bool AuditLogMutationField1(string arg1) => throw new NotImplementedException();
        }
        private readonly GeexWebApplicationFactory _factory;

        public AuditLogTests(GeexWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task AuditFieldsImplicitlyShouldWork()
        {
            // Arrange
            var service = _factory.Services;
            var schema = service.GetService<ISchema>();
            schema.MutationType.Fields.TryGetField("auditLogMutationField1", out var field1).ShouldBeTrue();
            field1.Directives.Any(x=>x.Type.RuntimeType == typeof(AuditDirectiveType)).ShouldBeTrue();

            schema.MutationType.Fields.TryGetField("approveApproveEntity", out var field2).ShouldBeTrue();
            field2.Directives.Any(x=>x.Type.RuntimeType == typeof(AuditDirectiveType)).ShouldBeTrue();
        }
    }
}
