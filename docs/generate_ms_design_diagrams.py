# -*- coding: utf-8 -*-
"""As-built microservices design diagrams: (1) component view, (2) reimbursement-saga sequence."""
import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

OUT = os.path.dirname(os.path.abspath(__file__))
INK = "#0f172a"
PAL = {
    "client": ("#EEF2FF", "#6366F1"), "gw": ("#E0E7FF", "#4F46E5"),
    "svc": ("#DCFCE7", "#16A34A"), "agg": ("#DBEAFE", "#2563EB"),
    "band": ("#F8FAFC", "#CBD5E1"),
}


def box(ax, x, y, w, h, t, kind, fs=8, bold=False):
    f, e = PAL[kind]
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.1", lw=1.3, edgecolor=e, facecolor=f))
    ax.text(x + w / 2, y + h / 2, t, ha="center", va="center", fontsize=fs, color=INK, weight="bold" if bold else "normal")


def arrow(ax, x1, y1, x2, y2, t=None, color="#475569", dashed=False, fs=7):
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=1.4, ls="--" if dashed else "-", shrinkA=2, shrinkB=2))
    if t:
        ax.text((x1 + x2) / 2, (y1 + y2) / 2 + 1.1, t, ha="center", va="center", fontsize=fs, color=color, backgroundcolor="white")


def band(ax, x, y, w, h, label):
    f, e = PAL["band"]
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.15", lw=1.3, edgecolor=e, facecolor=f, linestyle="--"))
    ax.text(x + 0.7, y + h - 0.8, label, ha="left", va="top", fontsize=8.5, color="#334155", weight="bold")


# ============ Diagram 1: component view ============
fig, ax = plt.subplots(figsize=(13.5, 9.5))
ax.set_xlim(0, 100); ax.set_ylim(0, 100); ax.axis("off")
ax.text(50, 97.5, "Unified Rewards — As-Built Microservices (Component View)", ha="center", fontsize=14, weight="bold", color=INK)

box(ax, 28, 89, 44, 5, "API consumers — 4 micro-frontends (Employee/Manager/HR/Finance)", "client", bold=True)
arrow(ax, 50, 89, 50, 85.5)
box(ax, 22, 79.5, 56, 5.5, "API Gateway — YARP :5080 (→ Azure API Management)\nvalidates JWT · routes /api/** · per-tenant", "gw", fs=8, bold=True)
arrow(ax, 50, 79.5, 50, 75.5)

# Row 1
r1 = [("Employee Profile\n:5101  ▣ own DB", 6), ("Benefits Catalogue\n:5102  ▣ own DB", 29.5),
      ("Compensation Rules\n:5103 (NRules) ▣", 53), ("Reimbursement Workflow\n:5104 (saga) ▣", 76.5)]
for t, x in r1:
    box(ax, x, 64, 21, 9, t, "svc", fs=7.4, bold=True)
# Row 2
r2 = [("Document & Receipt\n:5105 (OCR) ▣", 6), ("Payroll Integration\n:5106 (Polly) ▣", 35),
      ("Reporting & Compliance\n:5107 (aggregator)", 64)]
for t, x in r2:
    kind = "agg" if "Reporting" in t else "svc"
    box(ax, x, 50, 24, 9, t, kind, fs=7.4, bold=True)

# Collaboration arrows
arrow(ax, 87, 64, 47, 59, "saga: request + poll settlement", color="#DB2777")        # Reimbursement -> Payroll
arrow(ax, 76, 50, 80, 59, "aggregate claims", color="#2563EB")                         # Reporting -> Reimbursement
arrow(ax, 76, 52, 47, 52, "aggregate settlements", color="#2563EB")                    # Reporting -> Payroll

band(ax, 4, 4, 92, 40 - 16, "")  # spacer band behind notes
ax.text(6, 24, "Identity:  JWT issued by Employee Profile (→ Microsoft Entra ID in production); validated by every service; the tenant_id claim scopes all data.",
        fontsize=8.5, color="#334155")
ax.text(6, 19, "Data:  database-per-service — each owns its SQLite DB locally (→ Azure SQL / Cosmos in production). No shared database. (Reporting is a derived read model.)",
        fontsize=8.5, color="#334155")
ax.text(6, 14, "Collaboration:  direct service-to-service HTTP locally (→ asynchronous Azure Service Bus events in production).",
        fontsize=8.5, color="#334155")
ax.text(6, 9, "Deploy:  one process per service locally (zero-install dotnet run) → one container per service on AKS with HPA in production.",
        fontsize=8.5, color="#334155")

fig.tight_layout()
fig.savefig(os.path.join(OUT, "as_built_microservices_components.png"), dpi=150, bbox_inches="tight")
plt.close(fig)

# ============ Diagram 2: reimbursement saga sequence ============
fig, ax = plt.subplots(figsize=(13.5, 10))
ax.set_xlim(0, 100); ax.set_ylim(0, 100); ax.axis("off")
ax.text(50, 98, "Reimbursement Saga — Sequence (as-built, via the gateway)", ha="center", fontsize=14, weight="bold", color=INK)

lanes = {"User\n(Employee/Finance)": 12, "API Gateway\n(YARP)": 37, "Reimbursement\nWorkflow": 63, "Payroll\nIntegration": 88}
for name, x in lanes.items():
    box(ax, x - 9, 90, 18, 5.5, name, "gw" if "Gateway" in name else ("svc" if ("Reimb" in name or "Payroll" in name) else "client"), fs=8, bold=True)
    ax.plot([x, x], [90, 6], color="#94a3b8", ls="--", lw=1)

U, G, R, P = lanes["User\n(Employee/Finance)"], lanes["API Gateway\n(YARP)"], lanes["Reimbursement\nWorkflow"], lanes["Payroll\nIntegration"]

def msg(y, x1, x2, t, dashed=False, color="#475569"):
    arrow(ax, x1, y, x2, y, t, color=color, dashed=dashed, fs=7.2)

def selfmsg(y, x, t, color="#475569"):
    ax.annotate("", xy=(x, y - 1.4), xytext=(x + 7, y - 1.4), arrowprops=dict(arrowstyle="-|>", color=color, lw=1.3))
    ax.plot([x, x + 7, x + 7], [y, y, y - 1.4], color=color, lw=1.3)
    ax.text(x + 7.5, y - 0.4, t, ha="left", va="center", fontsize=7, color=color)

msg(84, U, G, "POST /api/auth/login → JWT")
msg(79, U, G, "POST /api/claims  (Employee)")
msg(75, G, R, "create claim → Submitted")
msg(71, R, U, "201 Created", dashed=True)
msg(64, U, G, "POST /api/claims/{id}/approve  (Finance)")
msg(60, G, R, "Approve (state machine)")
msg(56, R, U, "200 Approved", dashed=True)
msg(49, U, G, "POST /api/claims/{id}/settle  (Finance)")
msg(45, G, R, "settle")
msg(41, R, P, "POST /api/settlements  (forward JWT)", color="#DB2777")
selfmsg(37, P, "enqueue → async worker → Polly retry → Succeeded", color="#DB2777")
msg(31, R, P, "GET /api/settlements/{id}  (poll until terminal)", color="#DB2777")
msg(27, P, R, "status: Succeeded", dashed=True, color="#DB2777")
selfmsg(23, R, "claim.Settle(reference) → Settled", color="#16A34A")
msg(17, R, U, "200 Settled  (history: Submitted→Approved→Settled)", dashed=True)

ax.text(50, 9, "Local: direct HTTP between services (orchestration).  Production: the settle step publishes an event to Azure Service Bus (choreography).",
        ha="center", fontsize=8, color="#64748b", style="italic")

fig.tight_layout()
fig.savefig(os.path.join(OUT, "reimbursement_saga_sequence.png"), dpi=150, bbox_inches="tight")
plt.close(fig)

print("SAVED components + sequence diagrams")
