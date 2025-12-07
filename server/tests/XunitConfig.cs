using Xunit;

// Disable parallel test execution because all tests share one Postgres Testcontainer
[assembly: CollectionBehavior(DisableTestParallelization = true)]