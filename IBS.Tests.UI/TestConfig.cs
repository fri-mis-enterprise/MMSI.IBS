// Disable parallel test execution for UI tests.
// Running multiple browser+server instances concurrently against a single PostgreSQL DB
// causes AJAX responses, JS cascade dropdowns, and network load states to race and timeout.
// Sequential execution is correct for end-to-end browser tests.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
