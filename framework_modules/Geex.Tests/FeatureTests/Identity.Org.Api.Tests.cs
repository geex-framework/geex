using Geex.Extensions.Identity;
using Geex.Extensions.Identity.Core.Entities;
using Geex.Extensions.Identity.Requests;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using Newtonsoft.Json;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class IdentityOrgApiTests
    {
        private readonly TestApplicationFactory _factory;
        private readonly string _graphqlEndpoint = "/graphql";

        public IdentityOrgApiTests(TestApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task QueryOrgsShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new
            {
                query = $$"""
                    query {
                        orgs(skip: 0, take: 10) {
                            items {
                                id
                                code
                                name
                                orgType
                                parentOrgCode
                                allParentOrgCodes
                                allSubOrgCodes
                                directSubOrgCodes
                            }
                            pageInfo {
                                hasPreviousPage
                                hasNextPage
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            responseData["data"]["orgs"]["totalCount"].GetValue<int>().ShouldBeGreaterThanOrEqualTo(0);
        }

        [Fact]
        public async Task FilterOrgsByCodeShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var targetOrgCode = $"testorg_{ObjectId.GenerateNewId()}";

            // First create an org with specific code
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            var org = await uow.Request(new CreateOrgRequest
            {
                Code = targetOrgCode,
                Name = $"Test Org {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$$"""
                    query {
                        orgs(skip: 0, take: 10, filter: { code: { eq: "{{{targetOrgCode}}}" }}) {
                            items {
                                id
                                code
                                name
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["orgs"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["code"]).ShouldBe(targetOrgCode);
        }

        [Fact]
        public async Task FilterOrgsByNameShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var targetOrgName = $"Unique Org Name {ObjectId.GenerateNewId()}";
            var orgCode = $"uniqueorg_{ObjectId.GenerateNewId()}";

            // First create an org with specific name
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            var org = await uow.Request(new CreateOrgRequest
            {
                Code = orgCode,
                Name = targetOrgName,
                OrgType = OrgTypeEnum.Default
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$$"""
                    query {
                        orgs(skip: 0, take: 10, filter: { name: { eq: "{{{targetOrgName}}}" }}) {
                            items {
                                id
                                code
                                name
                            }
                            totalCount
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["orgs"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);
            ((string)items[0]["name"]).ShouldBe(targetOrgName);
        }

        [Fact]
        public async Task CreateOrgMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var testOrgCode = $"neworg_{ObjectId.GenerateNewId()}";
            var testOrgName = $"New Organization {ObjectId.GenerateNewId()}";
            
            var request = new
            {
                query = $$$"""
                    mutation {
                        createOrg(request: {
                            code: "{{{testOrgCode}}}"
                            name: "{{{testOrgName}}}"
                            orgType: Default
                        }) {
                            id
                            code
                            name
                            orgType
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var createdOrg = responseData["data"]["createOrg"];
            ((string)createdOrg["code"]).ShouldBe(testOrgCode);
            ((string)createdOrg["name"]).ShouldBe(testOrgName);
        }

        [Fact]
        public async Task CreateSubOrgMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // First create a parent org
            var parentOrgCode = $"parentorg_{ObjectId.GenerateNewId()}";
            var parentOrg = await uow.Request(new CreateOrgRequest
            {
                Code = parentOrgCode,
                Name = $"Parent Org {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            });
            await uow.SaveChanges();

            var subOrgCode = $"{parentOrgCode}.suborg_{ObjectId.GenerateNewId()}";
            var subOrgName = $"Sub Organization {ObjectId.GenerateNewId()}";

            var request = new
            {
                query = $$$"""
                    mutation {
                        createOrg(request: {
                            code: "{{{subOrgCode}}}"
                            name: "{{{subOrgName}}}"
                            orgType: Default
                        }) {
                            id
                            code
                            name
                            parentOrgCode
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var createdOrg = responseData["data"]["createOrg"];
            ((string)createdOrg["code"]).ShouldBe(subOrgCode);
            ((string)createdOrg["parentOrgCode"]).ShouldBe(parentOrgCode);
        }

        [Fact]
        public async Task DeleteOrgMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // First create an org to delete
            var testOrgCode = $"deleteorg_{ObjectId.GenerateNewId()}";
            var org = await uow.Request(new CreateOrgRequest
            {
                Code = testOrgCode,
                Name = $"Delete Test Org {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$$"""
                    mutation {
                        deleteOrg(id: "{{{org.Id}}}")
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool deleteResult = (bool)responseData["data"]["deleteOrg"];
            deleteResult.ShouldBeTrue();
        }

        [Fact]
        public async Task FixUserOrgMutationShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            
            var request = new
            {
                query = $$"""
                    mutation {
                        fixUserOrg
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            bool fixResult = (bool)responseData["data"]["fixUserOrg"];
            fixResult.ShouldBeTrue();
        }

        [Fact]
        public async Task QueryOrgHierarchyShouldWork()
        {
            // Arrange
            var client = _factory.CreateClient();
            var service = _factory.Services;
            var uow = service.GetService<IUnitOfWork>();

            // Create parent and sub org
            var parentCode = $"parent_{ObjectId.GenerateNewId()}";
            var parentOrg = await uow.Request(new CreateOrgRequest
            {
                Code = parentCode,
                Name = $"Parent Org {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            });

            var subCode = $"{parentCode}.sub_{ObjectId.GenerateNewId()}";
            var subOrg = await uow.Request(new CreateOrgRequest
            {
                Code = subCode,
                Name = $"Sub Org {ObjectId.GenerateNewId()}",
                OrgType = OrgTypeEnum.Default
            });
            await uow.SaveChanges();

            var request = new
            {
                query = $$"""
                    query {
                        orgs(skip: 0, take: 100) {
                            items {
                                id
                                code
                                name
                                parentOrgCode
                                allParentOrgCodes
                                directSubOrgCodes
                                allSubOrgCodes
                            }
                        }
                    }
                    """
            };

            var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

            // Act
            var response = await client.PostAsync(_graphqlEndpoint, content);
            var (responseData, responseString) = await response.ParseGraphQLResponse();

            // Assert
            var items = responseData["data"]["orgs"]["items"].AsArray();
            
            var parentOrgResult = items.FirstOrDefault(o => ((string)o["code"]) == parentCode);
            var subOrgResult = items.FirstOrDefault(o => ((string)o["code"]) == subCode);
            
            parentOrgResult.ShouldNotBeNull();
            subOrgResult.ShouldNotBeNull();
            
            var directSubCodes = parentOrgResult["directSubOrgCodes"].AsArray();
            directSubCodes.Any(x => ((string)x) == subCode).ShouldBeTrue();
            
            ((string)subOrgResult["parentOrgCode"]).ShouldBe(parentCode);
        }
    }
}
