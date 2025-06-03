using Geex.Extensions.BackgroundJob;
using Geex.Extensions.BackgroundJob.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Shouldly;

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyCronJob.Abstractions;
using MongoDB.Bson;

namespace Geex.Tests.FeatureTests
{
    [Collection(nameof(TestsCollection))]
    public class BackgroundJobServiceTests : TestsBase
    {
        public BackgroundJobServiceTests(TestApplicationFactory factory) : base(factory)
        {
        }

        [Fact]
        public async Task TestCronJobShouldBeRegistered()
        {
            // Arrange & Act
            using var scope = ScopedService.CreateScope();
            var hostedServices = scope.ServiceProvider.GetServices<IHostedService>();

            // Assert
            hostedServices.ShouldNotBeEmpty();
            hostedServices.Any(x => x.GetType().IsSubclassOf(typeof(CronJobService))).ShouldBeTrue();
        }

        [Fact]
        public async Task FireAndForgetTaskSchedulerShouldWork()
        {
            // Arrange
            var testData = $"test_data_{ObjectId.GenerateNewId()}";
            var testTask = new TestFireAndForgetTask(testData);

            // Act
            using var scope = ScopedService.CreateScope();
            var scheduler = scope.ServiceProvider.GetService<FireAndForgetTaskScheduler>();
            scheduler.ShouldNotBeNull();

            await scheduler.Schedule(testTask);

            // Give some time for the task to execute
            await Task.Delay(100);

            // Assert
            TestFireAndForgetTask.LastExecutedData.ShouldBe(testData);
        }

        [Fact]
        public async Task StatefulCronJobShouldMaintainState()
        {
            // Arrange
            var testJobState = new TestJobState { JobName = nameof(TestStatefulCronJob) };
            string jobId;

            using (var scope = ScopedService.CreateScope())
            {
                var uow = scope.ServiceProvider.GetService<IUnitOfWork>();
                uow.Attach(testJobState);
                testJobState.ExecutionCount = 5;
                await uow.SaveChanges();
                jobId = testJobState.Id;
            }

            // Act
            var testJob = new TestStatefulCronJob(ScopedService, "* * * * *");
            await testJob.Run(ScopedService, CancellationToken.None);

            // Assert
            using (var verifyScope = ScopedService.CreateScope())
            {
                var verifyUow = verifyScope.ServiceProvider.GetService<IUnitOfWork>();
                var updatedState = verifyUow.Query<TestJobState>().FirstOrDefault(x => x.Id == jobId);
                updatedState.ShouldNotBeNull();
                updatedState.ExecutionCount.ShouldBe(6);
            }
        }

        [Fact]
        public async Task CronJobShouldHandleExceptions()
        {
            // Arrange
            var testJob = new TestExceptionCronJob(ScopedService, "* * * * *");

            // Act & Assert - Should not throw
            await testJob.DoWork(CancellationToken.None);

            // Verify the exception was handled
            TestExceptionCronJob.ExceptionHandled.ShouldBeTrue();
        }

        [Fact]
        public async Task ConcurrentCronJobShouldAllowMultipleExecutions()
        {
            // Arrange
            var testJob = new TestConcurrentCronJob(ScopedService, "* * * * *");

            // Act
            var task1 = testJob.DoWork(CancellationToken.None);
            var task2 = testJob.DoWork(CancellationToken.None);

            await Task.WhenAll(task1, task2);

            // Assert
            TestConcurrentCronJob.ExecutionCount.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Fact]
        public async Task NonConcurrentCronJobShouldBlockSecondExecution()
        {
            // Arrange
            var testJob = new TestNonConcurrentCronJob(ScopedService, "* * * * *");

            // Act
            var task1 = Task.Run(() => testJob.DoWork(CancellationToken.None));
            await Task.Delay(50); // Ensure first task starts
            var task2 = Task.Run(() => testJob.DoWork(CancellationToken.None));

            await Task.WhenAll(task1, task2);

            // Assert - Second execution should be skipped
            TestNonConcurrentCronJob.ExecutionCount.ShouldBe(1);
        }

        [Fact]
        public async Task FireAndForgetTaskWithSameIdShouldNotScheduleTwice()
        {
            // Arrange
            var taskId = ObjectId.GenerateNewId().ToString();
            var task1 = new TestFireAndForgetTaskWithId(taskId, "data1");
            var task2 = new TestFireAndForgetTaskWithId(taskId, "data2");

            using var scope = ScopedService.CreateScope();
            var scheduler = scope.ServiceProvider.GetService<FireAndForgetTaskScheduler>();

            // Act
            await scheduler.Schedule(task1);
            await scheduler.Schedule(task2); // Should be ignored

            await Task.Delay(200);

            // Assert - Only first task should execute
            TestFireAndForgetTaskWithId.ExecutionCount.ShouldBe(1);
            TestFireAndForgetTaskWithId.LastExecutedData.ShouldBe("data1");
        }

        [Fact]
        public async Task FireAndForgetTaskCanBeCancelled()
        {
            // Arrange
            var taskId = ObjectId.GenerateNewId().ToString();
            var task = new TestLongRunningFireAndForgetTask(taskId);

            using var scope = ScopedService.CreateScope();
            var scheduler = scope.ServiceProvider.GetService<FireAndForgetTaskScheduler>();

            // Act
            await scheduler.Schedule(task);
            await Task.Delay(50); // Let it start
            scheduler.Cancel(taskId);
            await Task.Delay(200); // Wait for cancellation

            // Assert
            TestLongRunningFireAndForgetTask.WasCancelled.ShouldBeTrue();
        }
    }

    // Test helper classes
    public class TestFireAndForgetTask : FireAndForgetTask<string>
    {
        public static string LastExecutedData { get; set; }

        public TestFireAndForgetTask(string param) : base(param)
        {
        }

        public override async Task Run(CancellationToken token)
        {
            await Task.Delay(50, token);
            LastExecutedData = Param;
        }
    }

    public class TestFireAndForgetTaskWithId : FireAndForgetTask<string>
    {
        public static int ExecutionCount { get; set; }
        public static string LastExecutedData { get; set; }

        public TestFireAndForgetTaskWithId(string id, string param) : base(param)
        {
            Id = id;
        }

        public override string Id { get; }

        public override async Task Run(CancellationToken token)
        {
            await Task.Delay(50, token);
            ExecutionCount++;
            LastExecutedData = Param;
        }
    }

    public class TestLongRunningFireAndForgetTask : FireAndForgetTask<string>
    {
        public static bool WasCancelled { get; set; }

        public TestLongRunningFireAndForgetTask(string id) : base("")
        {
            Id = id;
        }

        public override string Id { get; }

        public override async Task Run(CancellationToken token)
        {
            try
            {
                await Task.Delay(10000, token); // Long running task
            }
            catch (OperationCanceledException)
            {
                WasCancelled = true;
                throw;
            }
        }
    }

    public class TestJobState : JobState
    {
        public int ExecutionCount { get; set; }
    }

    public class TestStatefulCronJob : StatefulCronJob<TestJobState, TestStatefulCronJob>
    {
        public TestStatefulCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {
        }

        public override bool IsConcurrentAllowed => false;

        public override async Task Run(IServiceProvider serviceProvider, TestJobState jobState, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            jobState.ExecutionCount++;
        }
    }

    public class TestExceptionCronJob : CronJob<TestExceptionCronJob>
    {
        public static bool ExceptionHandled { get; set; }

        public TestExceptionCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {
        }

        public override bool IsConcurrentAllowed => false;

        public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Delay(10, cancellationToken);
            throw new InvalidOperationException("Test exception");
        }

        protected override async Task OnException(Exception exception)
        {
            ExceptionHandled = true;
            await base.OnException(exception);
        }
    }

    public class TestConcurrentCronJob : CronJob<TestConcurrentCronJob>
    {
        public static int ExecutionCount;

        public TestConcurrentCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {
        }

        public override bool IsConcurrentAllowed => true;

        public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Delay(100, cancellationToken);
            Interlocked.Increment(ref ExecutionCount);
        }
    }

    public class TestNonConcurrentCronJob : CronJob<TestNonConcurrentCronJob>
    {
        public static int ExecutionCount;

        public TestNonConcurrentCronJob(IServiceProvider sp, string cronExp) : base(sp, cronExp)
        {
        }

        public override bool IsConcurrentAllowed => false;

        public override async Task Run(IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            await Task.Delay(200, cancellationToken);
            Interlocked.Increment(ref ExecutionCount);
        }
    }
}
