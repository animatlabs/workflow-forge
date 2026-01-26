## Competitive Benchmark Summary (Median)

**Update Note**: Benchmarks will be refreshed after the upcoming rerun. Figures below reflect the last completed run.

| Scenario | WorkflowForge | Workflow Core | Elsa |
|----------|---------------|---------------|------|
| Sequential (10 ops) | 231us | 8,594us | 20,898us |
| Data Passing (10 ops) | 267us | 9,037us | 19,747us |
| Conditional (10 ops) | 259us | 8,840us | 20,183us |
| Loop (50 items) | 186us | 31,914us | 65,590us |
| Concurrent (8 workflows) | 305us | 45,532us | 104,863us |
| Error Handling | 110us | 1,473us | 8,407us |
| Creation Overhead | 6.7us | 871us | 2,568us |
| Complete Lifecycle | 36us | N/A | 10,713us |

## Competitive Memory Summary

| Scenario | WorkflowForge | Workflow Core | Elsa |
|----------|---------------|---------------|------|
| Sequential (10 ops) | 12.84KB | 425.82KB | 2,969KB |
| Data Passing (10 ops) | 13.63KB | 426.09KB | 2,969KB |
| Conditional (10 ops) | 12.84KB | 425.82KB | 2,969KB |
| Loop (50 items) | 7.27KB | 2,118KB | 10,870KB |
| Concurrent (8 workflows) | 87.93KB | 3,230KB | 19,105KB |
| Error Handling | 7.52KB | 214KB | 2,006KB |
| Creation Overhead | 1.73KB | 127.38KB | 572.39KB |
| Complete Lifecycle | 3.72KB | N/A | 1,508KB |

Notes:
- WorkflowCore excluded from Complete Lifecycle due to architectural incompatibility.
- Results captured on Windows 11, .NET 8.0.21, 25 iterations.
