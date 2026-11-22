// AppConfig uses static, process-wide file paths (Application.StartupPath-based), and
// tests that inject state into the fake engine via an environment variable rely on the
// current process's environment not being mutated concurrently by another test. Running
// this assembly's tests sequentially avoids both hazards without needing per-test locking.
[assembly: CollectionBehavior(MaxParallelThreads = 1)]
