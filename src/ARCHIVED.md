# Archived — Original Monolith

The projects in this directory (`UnifiedRewards.Domain`, `UnifiedRewards.Application`,
`UnifiedRewards.Infrastructure`, `UnifiedRewards.Api`) and the companion test project
(`tests/UnifiedRewards.UnitTests`) are the **original modular monolith** built in the
first phase of the Unified Rewards Platform.

They have been superseded by the microservices under `services/` as part of the
monolith-to-microservices migration documented in
`docs/Unified_Rewards_Platform_Microservices_Migration_Plan.docx`.

These files are retained **for git history only** and are no longer included in
`UnifiedRewards.sln`. Do not build or deploy them.

| Replaced by | Location |
|---|---|
| UnifiedRewards.Domain | Domain models in each `services/<name>/…/Domain/` |
| UnifiedRewards.Application | Service layer in each `services/<name>/…/` |
| UnifiedRewards.Infrastructure | Persistence in each `services/<name>/…/Persistence/` |
| UnifiedRewards.Api | Controllers in each `services/<name>/…/Controllers/` |
| UnifiedRewards.UnitTests | Tests in each `services/<name>/…Tests/` (where present) |
