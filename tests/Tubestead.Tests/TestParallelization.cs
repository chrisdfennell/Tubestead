// The setting-precedence tests mutate process-wide environment variables, and the
// integration host reads those same variables. Run tests sequentially so they
// can't observe each other's env mutations.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
