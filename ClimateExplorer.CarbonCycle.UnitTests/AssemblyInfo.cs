using Microsoft.VisualStudio.TestTools.UnitTesting;

// Test classes run in parallel; the methods within a class run one at a time.
[assembly: Parallelize(Scope = ExecutionScope.ClassLevel)]
