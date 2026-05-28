# SMART_ASSIGNMENT_ENGINE

## Lógica
Carga comercial = leads abiertos + deals open por usuario.  
Asigna al rep con **menor carga** (`GetRecommendedOwnerAsync`).

## Automación
`Lead.ScoreUpdated` con score ≥ 70 → `AssignLeadToBestRepAsync` (si no tenía owner).

## API indirecta
Resultado visible en leaderboard y asignación de leads.

## Futuro (Fase 13+)
Territorio, industria, disponibilidad calendario.
