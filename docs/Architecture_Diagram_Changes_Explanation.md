# Architecture Diagram Changes — Previous (Review) vs Current

**Purpose:** Explain what changed between the architecture diagrams presented in the earlier review
call (the "previous" set, supplied as images) and the current diagrams in `docs/Diagrams/`, **why** each
change was made, and which decisions trace back to the **Requirement Document**.

---

## 1. Executive summary — did we make major changes?

| Diagram | Change | Severity |
|---|---|---|
| **High-Level Architecture** | Added the **Promotions** capability (was missing); Employee→User Mgmt, Reporting→Reporting & Audit; Payroll surfaced as a controller | **Major (content)** |
| **Deployment Architecture (Azure)** | Same service-decomposition update; **Azure infrastructure/topology unchanged** | Major (services) / none (infra) |
| **Logical & Functional Architecture views** | Same update, kept consistent with the above | Aligned |
| **Detailed diagrams** (Class, Component, ER, Sequence) | Regenerated/updated alongside the build | Supporting |

**Bottom line:** One **major content change** — the diagrams now include **Promotions (bonus/reward
campaigns)**, which the originally-presented diagrams **omitted entirely** even though it is a mandated
requirement. Promotions is shown as a capability of the **Employee Profile (User Mgmt) service**, so the
diagrams now match the **7 requirement-named services** and the codebase. The **Azure
infrastructure/topology (AKS, APIM, Azure SQL, Cosmos, Redis, Front Door, …) is unchanged.**

---

## 2. The change — applied consistently across all architecture views

The current set of services (High-Level, Deployment, Logical, Functional all agree):

> **User Mgmt** (Employee Profile, *incl. Promotions*) · **Benefits** · **Compensation** · **Claims**
> (Reimbursement Workflow) · **Documents** (Document Processing) · **Payroll** · **Reporting & Audit**

| Aspect | Previous (review) | Current (`/docs`) | Why |
|---|---|---|---|
| **Promotions** | *Absent from every diagram* | **Added** — shown inside **User Mgmt / Employee Profile** | **Requirement** mandates promotion/bonus management; it was missing |
| **API controllers** | **6** (Employee, Benefits, Compensation, Claims, Documents, Reports) | **7** (User Mgmt, Benefits, Compensation, Claims, **Payroll**, Documents, Reports) | Payroll surfaced as its own controller |
| **Document Processing** | Separate service | **Separate service (unchanged)** | Matches the requirement's 7 named services |
| **Naming** | Employee · Reporting | **User Mgmt** · **Reporting & Audit** | Align names with the requirement's capability language |

> **Key point:** the headline change is **adding Promotions**. The 7-service decomposition — including
> **Document Processing as a separate service** — is preserved, so the diagrams stay faithful to the
> requirement and the code (where Promotions lives in the Employee Profile service as `PromotionsController`).

---

## 3. Why — grounded in the Requirement Document

The changes close a gap against the **Requirement Document** and the **Requirement Analysis &
Clarification** document:

- **Promotions = reward/bonus campaigns (not career grade-changes).** Explicitly required:
  - *"System must allow HR admins to manage active promotions and bonus schemes (e.g., Year-End Bonus,
    Diwali Voucher)."*
  - *"Promotion and bonus configuration data (promotion name, value, applicable period, eligibility
    rules)."*
  - HR Admin actor: *"Configures benefit plans, **manages promotions**, sets compensation rules."*
  - The earlier diagrams modelled none of this, so **Promotions** was added (within Employee Profile) to
    match the mandated scope.

- **Seven named services preserved.** The requirement names the seven services — Employee Profile,
  Benefits Catalogue, Compensation Rules, Reimbursement Workflow, **Document Processing**, Payroll
  Integration, Reporting & Compliance — so **Document Processing stays a separate service** in the
  diagrams, matching the code.

- **Payroll made explicit.** Requirement: *"integrate with external payroll systems for compensation
  processing and settlement."* Payroll is now its own controller (payslip on-demand + async settlement).

- **Reporting & Audit naming.** Requirement: *"Full audit trail of all claim submissions, approvals, and
  benefits transactions"* — the rename surfaces the audit responsibility that was previously implicit.

---

## 4. What did NOT change — the Azure infrastructure topology

Only the *application service boxes* were updated. The **Deployment Architecture's infrastructure is
unchanged** and still matches the **HLD**:

- **AKS** cluster (HPA 2–10 pods/service) on a private VNet · **APIM** gateway · **Azure Front Door**
  (WAF/CDN) → **Static Web Apps** (micro-frontends).
- **Azure SQL** · **Cosmos DB** (multi-region) · **Redis** Premium · **Blob** (GRS) · **Key Vault** (MSI).
- **Service Bus** (events) · **Event Hub** (audit) · **Azure Functions** (OCR/timers/payroll) ·
  **Communication Services** (email) · **Entra ID** SSO/MFA · **Azure Monitor / App Insights**.

---

## 5. Consistency — diagrams now match the requirement & code

After this update, all four architecture views (High-Level, Deployment, Logical, Functional) show the
**same seven services**, and they line up with:

- **The Requirement Document** — the seven named services, with Promotions as an HR-admin capability.
- **The codebase** — `Document Processing` is a standalone service; `PromotionsController` lives inside
  the `Employee Profile` service.

There is **no remaining logical-vs-physical or diagram-vs-code mismatch** in the service decomposition.

---

## 6. Talking points for the review

1. **"We added Promotions."** The earlier diagrams missed a mandated capability (HR-managed bonus/reward
   campaigns — Year-End Bonus, Diwali Voucher). It's now shown as part of the Employee Profile service.
2. **"We kept the 7 named services."** Document Processing remains a separate service; we did **not**
   merge it into Claims, so the diagrams match the requirement and the code.
3. **"We tidied naming."** Employee→User Mgmt, Reporting→Reporting & Audit, and Payroll is now an explicit
   controller (sync payslip + async settlement).
4. **"The Azure platform design is stable."** No change to the production topology (AKS/APIM/Azure
   SQL/Cosmos/Redis/Front Door/multi-region) — it matches the HLD.

---

> *Separately from the diagram-vs-diagram changes:* the **as-built demo deployment** (Container Apps +
> SQLite + YARP + single consolidated SPA + self-issued JWT) differs from the diagram set, which depicts
> the production target (AKS + Azure SQL/Cosmos + APIM + Static Web Apps + Entra). That
> design-vs-implementation gap is tracked separately and is not part of this diagram-change comparison.
