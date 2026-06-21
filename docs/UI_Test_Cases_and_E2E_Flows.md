# Unified Rewards Platform — UI Test Cases & End-to-End Flows

Manual UI test pack: end-to-end flows plus per-module **positive and negative** test cases.

---

## 1. Test environment & prerequisites

| Item | Value |
|---|---|
| App URL | Your deployed Static Web App URL (e.g. `https://<name>.azurestaticapps.net`) or `http://localhost:3000` locally |
| Backend | Gateway URL must be reachable + CORS allows the site (see Deployment guide) |
| Browser | Latest Chrome/Edge; use an Incognito window per role to avoid token clashes |

**Seeded test accounts** (password for all: **`Password123!`**):

| Email | Role | Portal shown |
|---|---|---|
| `employee@urp.local` | Employee | Employee (Benefits, Claims, Payslips, Compensation, Bonus Campaigns) |
| `manager@urp.local` | Manager | Manager (Claims Review, Promotions) |
| `hr@urp.local` | HrAdmin | HR (Benefit Plans, Compensation, Promotions, Users, Audit) |
| `finance@urp.local` | Finance | Finance (Payroll, Settlements, Reports, Audit) |

**Conventions:** **P** = positive, **N** = negative. Claim statuses surface as labels: *Submitted → In Review → Approved → Settled* (or *Rejected*).

---

## 2. End-to-End flows

### E2E-1 — Reimbursement claim lifecycle (the core flow) ⭐
Covers Employee → Manager → Finance, three browser sessions.

| # | Role | Action | Expected |
|---|---|---|---|
| 1 | Employee | Log in (`employee@urp.local`) → **Claims** tab → fill **type**, **amount** (e.g. 2500), **description**, attach a receipt image → **Submit claim** | Claim appears in "My Claims" with status **Submitted**; a reference/ID is shown |
| 2 | Employee | Note the claim status | **Submitted** (or **In Review**) |
| 3 | Manager | Log in (`manager@urp.local`) → **Claims Review** → find the claim → **Approve** | Claim moves to **Approved**; disappears from the pending list |
| 4 | Finance | Log in (`finance@urp.local`) → **Settlements** → the approved claim is listed → **Settle** | Settlement queued; after a few seconds claim becomes **Settled** |
| 5 | Employee | Refresh **Claims** | Same claim now shows **Settled**; history shows Submitted → Approved → Settled with timestamps |

**Pass criteria:** the claim transitions Submitted → Approved → Settled, visible to all three roles, with a consistent history trail.

### E2E-2 — Reject path
| # | Role | Action | Expected |
|---|---|---|---|
| 1 | Employee | Submit a claim | Status **Submitted** |
| 2 | Manager | **Claims Review** → **Reject** (add notes) | Claim status **Rejected** |
| 3 | Finance | **Settlements** | The rejected claim is **not** in the settle list |
| 4 | Employee | View claim | Status **Rejected**, decision notes visible |

### E2E-3 — Benefit enrollment
| # | Role | Action | Expected |
|---|---|---|---|
| 1 | Employee | **Benefits** → pick an active plan → **Enroll** | Plan appears under "My Enrollments" (status Active) |
| 2 | Employee | **Cancel** the enrollment | Enrollment removed/Cancelled |

### E2E-4 — Promotion (bonus campaign)
| # | Role | Action | Expected |
|---|---|---|---|
| 1 | HR | **Promotions** → **+ New Campaign** → fill title, cycle year/quarter, bonus value, nomination dates → **Create** | Campaign created (status Draft) |
| 2 | HR | **Open** the campaign | Status **Open** (nominations allowed) |
| 3 | Manager | **Promotions** → select campaign → **Nominate** an employee (+ remarks) → **Submit Nomination** | Nomination recorded; nomination count +1 |
| 4 | HR | Open campaign nominations → **Approve** the nomination | Nomination **Approved**; approved count +1 |
| 5 | Employee | **Bonus Campaigns** | The employee sees their nomination + status |

### E2E-5 — Compensation & payslip
| # | Role | Action | Expected |
|---|---|---|---|
| 1 | HR | **Compensation** → enter employee, grade, annual basic, effective date → **Generate** → **Approve** | Compensation structure approved (CTC computed) |
| 2 | Finance | **Payroll** → **Generate payslip** for the employee (year/month) | Payslip created |
| 3 | Employee | **Compensation** / **Payslips** | CTC breakdown + payslip visible |

---

## 3. Module test cases

### 3.1 Authentication & Authorization
| TC | P/N | Steps | Expected |
|---|---|---|---|
| AUTH-01 | P | Enter `hr@urp.local` / `Password123!` → Sign in | HR portal loads; user name shown |
| AUTH-02 | P | Repeat for employee/manager/finance accounts | Each loads its own portal |
| AUTH-03 | N | Valid email, **wrong password** → Sign in | Error banner *"Invalid email or password"*; stays on login |
| AUTH-04 | N | **Unknown email** (`nobody@urp.local`) → Sign in | Same invalid-credentials error |
| AUTH-05 | N | Leave email or password **empty** → Sign in | Form blocks submit (required-field validation) |
| AUTH-06 | N | Enter a malformed email (`abc`) → Sign in | Browser email-format validation blocks submit |
| AUTH-07 | P | Log in, then **log out** | Returns to login; protected pages no longer accessible |
| AUTH-08 | N | Log in as Employee | **No** Manager/HR/Finance tabs are visible (role-scoped UI) |
| AUTH-09 | N | (API) Call a manager-only endpoint with an Employee token | **403 Forbidden** |
| AUTH-10 | N | (API) Call any `/api/...` endpoint **without** a token | **401 Unauthorized** |

### 3.2 Employee — Benefits
| TC | P/N | Steps | Expected |
|---|---|---|---|
| BEN-01 | P | Benefits tab loads | Active benefit plans list with name, category, monthly cost |
| BEN-02 | P | Click **Enroll** on a plan | Plan shows under "My Enrollments" |
| BEN-03 | P | Click **Cancel** on an enrollment | Enrollment removed/Cancelled |
| BEN-04 | N | **Enroll** in the same plan twice | Second attempt rejected or no duplicate created (one active enrollment) |
| BEN-05 | N | **Cancel** an already-cancelled enrollment | Handled gracefully (error banner / no-op, no crash) |

### 3.3 Employee — Claims (submit)
| TC | P/N | Steps | Expected |
|---|---|---|---|
| CLM-01 | P | Fill type + amount (2500) + description + receipt → **Submit claim** | Claim created, status **Submitted**, listed in My Claims |
| CLM-02 | P | Submit a claim **without** a receipt | Claim created (receipt optional) |
| CLM-03 | P | Submit, then view claim detail | Shows amount, description, status, history |
| CLM-04 | N | Amount = **0** or **negative** → Submit | Rejected (validation/error banner); no claim created |
| CLM-05 | N | **Empty description** → Submit | Blocked (required) or rejected |
| CLM-06 | N | Amount = non-numeric text | Number field rejects / Submit blocked |
| CLM-07 | N | Very large amount (e.g. 99999999) | Either accepted per business rule or rejected gracefully (no crash) |
| CLM-08 | N | Upload a non-image/oversized file as receipt | Rejected with a clear message |

### 3.4 Employee — Compensation / Payslips / Bonus Campaigns
| TC | P/N | Steps | Expected |
|---|---|---|---|
| EMP-01 | P | Open **Compensation** | CTC structure (gross, deductions, net, components) shown |
| EMP-02 | P | Open **Payslips** | Payslip(s) listed with gross/net/month |
| EMP-03 | P | Open **Bonus Campaigns** | My nominations + their status shown |
| EMP-04 | N | New employee with no comp data | Empty-state message, not an error/crash |

### 3.5 Manager — Claims Review
| TC | P/N | Steps | Expected |
|---|---|---|---|
| MGR-01 | P | Open Claims Review | Pending/submitted claims are listed |
| MGR-02 | P | **Approve** a submitted claim | Status → **Approved**; leaves the pending list |
| MGR-03 | P | **Reject** a submitted claim (with notes) | Status → **Rejected**; notes saved |
| MGR-04 | N | Approve a claim that is already **Settled/Rejected** | Action blocked by the state machine (error / not allowed) |
| MGR-05 | N | Approve with no claims present | Empty state; no action available |

### 3.6 Manager — Promotions (nominations)
| TC | P/N | Steps | Expected |
|---|---|---|---|
| MGR-06 | P | Select an **Open** campaign → **Nominate** an employee + remarks → Submit | Nomination recorded; count increases |
| MGR-07 | N | Nominate against a **Draft/Closed** campaign | Rejected (nominations only when Open) |
| MGR-08 | N | Submit nomination with **empty employee** | Blocked (required) |
| MGR-09 | N | Nominate the **same employee twice** in one campaign | Duplicate rejected or prevented |

### 3.7 HR — Benefit Plans
| TC | P/N | Steps | Expected |
|---|---|---|---|
| HR-01 | P | **Create plan** with name, description, category, monthly cost | Plan created; appears in the list and to employees |
| HR-02 | N | Create with **empty name** | Blocked/rejected |
| HR-03 | N | **Negative** monthly cost | Rejected with validation |
| HR-04 | N | Non-numeric cost | Number field rejects |

### 3.8 HR — Compensation
| TC | P/N | Steps | Expected |
|---|---|---|---|
| HR-05 | P | Generate a comp structure (employee, grade, annual basic, effective date) → **Approve** | Structure created then Approved; CTC computed |
| HR-06 | N | Generate with **annual basic = 0/negative** | Rejected |
| HR-07 | N | **Approve** an already-approved structure | Blocked / idempotent, no crash |

### 3.9 HR — Promotions (campaigns)
| TC | P/N | Steps | Expected |
|---|---|---|---|
| HR-08 | P | **+ New Campaign** → valid fields → Create | Campaign created (Draft) |
| HR-09 | P | **Open** then later **Close** a campaign | Status transitions Draft → Open → Closed |
| HR-10 | P | **Approve / Reject** a nomination | Nomination status updates; counts adjust |
| HR-11 | N | nomination **end date before start date** | Rejected with validation |
| HR-12 | N | **Negative** bonus value | Rejected |
| HR-13 | N | Approve nominations on a **Closed** campaign | Blocked appropriately |

### 3.10 HR — Users
| TC | P/N | Steps | Expected |
|---|---|---|---|
| HR-14 | P | **Create employee** (full name, email, password, grade, DOJ) | Employee created; can log in |
| HR-15 | N | **Duplicate email** (existing user) | Rejected ("already exists") |
| HR-16 | N | Invalid email format | Blocked (email field) |
| HR-17 | N | Weak/empty password | Rejected per policy |

### 3.11 HR / Finance — Audit
| TC | P/N | Steps | Expected |
|---|---|---|---|
| AUD-01 | P | Open Audit | Audit entries listed (event type, actor, time) |
| AUD-02 | P | Paste a valid **claim UUID** in search | Only that claim's audit trail is shown |
| AUD-03 | N | Paste an **invalid UUID / random text** | Empty result or validation message, no crash |

### 3.12 Finance — Settlements & Payroll
| TC | P/N | Steps | Expected |
|---|---|---|---|
| FIN-01 | P | **Settlements** lists **Approved** claims → **Settle** one | Settlement queued; claim becomes **Settled** |
| FIN-02 | N | Settle when there are **no approved** claims | Empty list; nothing to settle |
| FIN-03 | N | **Settle the same claim twice** (quickly) | Second attempt blocked/idempotent |
| FIN-04 | P | **Payroll** → **Generate payslip** (employee, year, month, amounts) | Payslip generated |
| FIN-05 | P | **Payroll** → **Queue settlement** (employee, amount, reference) | Settlement request created |
| FIN-06 | N | Generate payslip with no employee selected | Button disabled / blocked |
| FIN-07 | N | Queue settlement with amount 0/negative | Rejected |

### 3.13 Finance — Reports
| TC | P/N | Steps | Expected |
|---|---|---|---|
| RPT-01 | P | Open Reports dashboard | Shows Total claims, Total settlements, etc. |
| RPT-02 | P | **Export claims (.xlsx)** | A valid `.xlsx` downloads and opens |
| RPT-03 | P | Submit/approve/settle a claim, then refresh dashboard | Totals update accordingly |

---

## 4. Cross-cutting / non-functional checks
| TC | P/N | Steps | Expected |
|---|---|---|---|
| NFR-01 | N | Let the session sit until the JWT expires, then act | Redirected to login / 401 surfaced gracefully |
| NFR-02 | N | Stop the backend, then use the UI | Friendly error banner ("Is the gateway running…"), no white screen |
| NFR-03 | N | Submit a form twice fast (double-click) | No duplicate record (button disables while busy) |
| NFR-04 | P | Reload the page mid-session | Stays logged in (token persisted) until expiry |
| NFR-05 | N | Directly open a deep link without logging in | Login is required first |
| NFR-06 | P | Resize to mobile width | Layout remains usable (no broken overflow) |

---

## 5. Test data & cleanup
- Use the four seeded `*@urp.local` accounts. Create extra employees via **HR → Users** if you need more.
- After E2E runs, claims/enrollments/campaigns remain in the DB — clean up via the API or HR/Finance screens if you want a fresh state.
- **Note:** if the backend uses SQLite on ephemeral storage, a replica restart resets data (seeded accounts re-seed). On the Azure SQL upgrade, data persists.

---

## 6. Suggested execution order
1. **AUTH** (all roles can log in) → 2. **E2E-1** (core claim flow) → 3. **E2E-2..5** (other flows) →
4. Module **negative** cases → 5. **NFR** checks. Record Pass/Fail + a screenshot per failure.
