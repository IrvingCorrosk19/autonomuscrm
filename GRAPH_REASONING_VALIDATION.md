# Graph Reasoning Validation

## Before (FALSO)

`GraphReasoningEngine.cs` returned fixed confidence: 0.82, 0.55, 0.78, 0.85, 0.76, 0.75, 0.8, 0.7, 0.4, 0.45

## After (REAL)

**`GraphConfidenceCalculator.Calculate(GraphConfidenceInput)`** factors:
- Evidence count (max +0.30)
- Edge count (max +0.18)
- Relationship strength (max +0.12)
- Positive/negative outcome ratio (max +0.22)
- Recency buckets (7d/30d/90d)
- Semantic match score (max +0.06)
- Temporal relevance (max +0.08)
- Clamped [0.05, 0.98]

**`GraphReasoningEngine`** uses calculator per method; loads outcomes via `BusinessMemoryRoots` → `BusinessMemoryOutcomes` join.

## Tests

`GraphConfidenceCalculatorTests.cs` — 12 tests  
`PhaseDGraphReasoningTests.cs` — confidence not equal to 0.82/0.55

## Evidence

No literal 0.82/0.55/0.78 in `GraphReasoningEngine.cs` (grep verified post-sprint)
