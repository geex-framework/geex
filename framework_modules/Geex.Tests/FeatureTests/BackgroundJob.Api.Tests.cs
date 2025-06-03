using Geex.Extensions.BackgroundJob;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Entities;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BackgroundJobApiTests : TestsBase
    {
        public BackgroundJobApiTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task QueryJobStatesShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testJobName = $"TestJob_{ObjectId.GenerateNewId()}";
            
            // Prepare test data
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var jobState = uow.Attach(new TestApiJobState()
                {
                    JobName = testJobName,
                    Status = "Running",
                    LastExecutionTime = DateTime.UtcNow
                });
                await uow.SaveChanges();
            }

            var query = """
                query {
                    jobStates(skip: 0, take: 10) {
                        items { id jobName status lastExecutionTime }
                        pageInfo { hasPreviousPage hasNextPage }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query);

            // Assert
            int totalCount = responseData["data"]["jobStates"]["totalCount"].GetValue<int>();
            totalCount.ShouldBeGreaterThanOrEqualTo(1);

            var items = responseData["data"]["jobStates"]["items"].AsArray();
            items.Any(item => (string)item["jobName"] == testJobName).ShouldBeTrue();
        }

        [Fact]
        public async Task FilterJobStatesByJobNameShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var specificJobName = $"SpecificJob_{ObjectId.GenerateNewId()}";
            
            // Prepare test data
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var jobState = uow.Attach(new TestApiJobState()
                {
                    JobName = specificJobName,
                    Status = "Completed",
                    LastExecutionTime = DateTime.UtcNow
                });
                await uow.SaveChanges();
            }

            var query = """
                query($jobName: String!) {
                    jobStates(skip: 0, take: 10, filter: { jobName: { eq: $jobName } }) {
                        items { id jobName status }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query, new { jobName = specificJobName });

            // Assert
            var items = responseData["data"]["jobStates"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["jobName"]).ShouldBe(specificJobName);
            }
        }

        [Fact]
        public async Task FilterJobStatesByStatusShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testJobName = $"StatusTestJob_{ObjectId.GenerateNewId()}";
            var targetStatus = "Failed";
            
            // Prepare test data
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                var jobState = uow.Attach(new TestApiJobState()
                {
                    JobName = testJobName,
                    Status = targetStatus,
                    LastExecutionTime = DateTime.UtcNow,
                    ErrorMessage = "Test error message"
                });
                await uow.SaveChanges();
            }

            var query = """
                query($status: String!) {
                    jobStates(skip: 0, take: 10, filter: { status: { eq: $status } }) {
                        items { id jobName status errorMessage }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query, new { status = targetStatus });

            // Assert
            var items = responseData["data"]["jobStates"]["items"].AsArray();
            items.Count.ShouldBeGreaterThan(0);

            foreach (var item in items)
            {
                ((string)item["status"]).ShouldBe(targetStatus);
            }
        }

        [Fact]
        public async Task TriggerFireAndForgetTaskMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var taskData = $"api_trigger_test_{ObjectId.GenerateNewId()}";

            var mutation = """
                mutation($taskType: String!, $taskData: String!) {
                    triggerFireAndForgetTask(request: { taskType: $taskType, taskData: $taskData })
                }
                """;

            // Act
            var (responseData, _) = await client.PostGqlRequest(GqlEndpoint, mutation, new 
            { 
                taskType = nameof(TestApiFireAndForgetTask),
                taskData = taskData
            });

            // Assert
            bool triggerResult = (bool)responseData["data"]["triggerFireAndForgetTask"];
            triggerResult.ShouldBeTrue();

            // Wait a bit for task execution
            await Task.Delay(200);
            
            // Verify task was executed
            TestApiFireAndForgetTask.LastExecutedData.ShouldBe(taskData);
        }

        [Fact]
        public async Task CancelFireAndForgetTaskMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var taskId = ObjectId.GenerateNewId().ToString();

            // First trigger a long-running task
            using (var scope = ScopedService.CreateScope())
            {
                var scheduler = scope.ServiceProvider.GetService<FireAndForgetTaskScheduler>();
                var task = new TestApiLongRunningTask(taskId);
                await scheduler.Schedule(task);
            }

            var mutation = """
                mutation($taskId: String!) {
                    cancelFireAndForgetTask(request: { taskId: $taskId })
                }
                """;

            // Act
            var (responseData, _) = await client.PostGqlRequest(GqlEndpoint, mutation, new { taskId });

            // Assert
            bool cancelResult = (bool)responseData["data"]["cancelFireAndForgetTask"];
            cancelResult.ShouldBeTrue();
        }

        [Fact]
        public async Task QueryJobExecutionHistoryShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var testJobName = $"HistoryTestJob_{ObjectId.GenerateNewId()}";
            
            // Prepare test data - multiple executions
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                for (int i = 0; i < 3; i++)
                {
                    var jobExecution = uow.Attach(new TestJobExecutionHistory()
                    {
                        JobName = testJobName,
                        ExecutionStartTime = DateTime.UtcNow.AddMinutes(-i),
                        ExecutionEndTime = DateTime.UtcNow.AddMinutes(-i).AddSeconds(30),
                        Status = i == 0 ? "Failed" : "Completed",
                        ErrorMessage = i == 0 ? "Test error" : null
                    });
                }
                await uow.SaveChanges();
            }

            var query = """
                query($jobName: String!) {
                    jobExecutionHistory(skip: 0, take: 10, filter: { jobName: { eq: $jobName } }) {
                        items { 
                            id 
                            jobName 
                            executionStartTime 
                            executionEndTime 
                            status 
                            errorMessage 
                        }
                        totalCount
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query, new { jobName = testJobName });

            // Assert
            var items = responseData["data"]["jobExecutionHistory"]["items"].AsArray();
            items.Count.ShouldBe(3);
            
            // Verify all items belong to our test job
            foreach (var item in items)
            {
                ((string)item["jobName"]).ShouldBe(testJobName);
            }
            
            // Verify we have both successful and failed executions
            items.Any(item => (string)item["status"] == "Failed").ShouldBeTrue();
            items.Any(item => (string)item["status"] == "Completed").ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateJobConfigurationMutationShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            var jobName = "TestConfigJob";
            var newCronExpression = "0 0 2 * * ?"; // Daily at 2 AM

            var mutation = """
                mutation($jobName: String!, $cronExpression: String!, $isEnabled: Boolean!) {
                    updateJobConfiguration(request: { 
                        jobName: $jobName, 
                        cronExpression: $cronExpression,
                        isEnabled: $isEnabled
                    })
                }
                """;

            // Act
            var (responseData, _) = await client.PostGqlRequest(GqlEndpoint, mutation, new 
            { 
                jobName = jobName,
                cronExpression = newCronExpression,
                isEnabled = true
            });

            // Assert
            bool updateResult = (bool)responseData["data"]["updateJobConfiguration"];
            updateResult.ShouldBeTrue();

            // Verify the configuration was updated
            using (var verifyScope = ScopedService.CreateScope())
            {
                var uow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var config = uow.Query<TestJobConfiguration>().FirstOrDefault(x => x.JobName == jobName);
                config.ShouldNotBeNull();
                config.CronExpression.ShouldBe(newCronExpression);
                config.IsEnabled.ShouldBeTrue();
            }
        }

        [Fact]
        public async Task GetJobStatisticsShouldWork()
        {
            // Arrange
            var client = this.SuperAdminClient;
            
            // Prepare test data with various job states
            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                
                var runningJob = uow.Attach(new TestApiJobState()
                {
                    JobName = $"RunningJob_{ObjectId.GenerateNewId()}",
                    Status = "Running"
                });
                
                var completedJob = uow.Attach(new TestApiJobState()
                {
                    JobName = $"CompletedJob_{ObjectId.GenerateNewId()}",
                    Status = "Completed"
                });
                
                var failedJob = uow.Attach(new TestApiJobState()
                {
                    JobName = $"FailedJob_{ObjectId.GenerateNewId()}",
                    Status = "Failed"
                });
                
                await uow.SaveChanges();
            }

            var query = """
                query {
                    jobStatistics {
                        totalJobs
                        runningJobs
                        completedJobs
                        failedJobs
                        scheduledJobs
                    }
                }
                """;

            // Act
            var (responseData, responseString) = await client.PostGqlRequest(GqlEndpoint, query);

            // Assert
            var stats = responseData["data"]["jobStatistics"];
            
            int totalJobs = stats["totalJobs"].GetValue<int>();
            int runningJobs = stats["runningJobs"].GetValue<int>();
            int completedJobs = stats["completedJobs"].GetValue<int>();
            int failedJobs = stats["failedJobs"].GetValue<int>();
            
            totalJobs.ShouldBeGreaterThan(0);
            runningJobs.ShouldBeGreaterThanOrEqualTo(1);
            completedJobs.ShouldBeGreaterThanOrEqualTo(1);
            failedJobs.ShouldBeGreaterThanOrEqualTo(1);
        }
    }

    // Test helper classes for API tests
    public class TestApiJobState : JobState
    {
        public string Status { get; set; }
        public DateTime? LastExecutionTime { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TestJobExecutionHistory : EntityBase<TestJobExecutionHistory>
    {
        public string JobName { get; set; }
        public DateTime ExecutionStartTime { get; set; }
        public DateTime? ExecutionEndTime { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class TestJobConfiguration : EntityBase<TestJobConfiguration>
    {
        public string JobName { get; set; }
        public string CronExpression { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class TestApiFireAndForgetTask : FireAndForgetTask<string>
    {
        public static string LastExecutedData { get; set; }
        
        public TestApiFireAndForgetTask(string param) : base(param)
        {
        }

        public override async Task Run(CancellationToken token)
        {
            await Task.Delay(50, token);
            LastExecutedData = Param;
        }
    }

    public class TestApiLongRunningTask : FireAndForgetTask<string>
    {
        public TestApiLongRunningTask(string id) : base("")
        {
            Id = id;
        }
        
        public override string Id { get; }

        public override async Task Run(CancellationToken token)
        {
            try
            {
                await Task.Delay(5000, token); // 5 second task
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
                throw;
            }
        }
    }
}
