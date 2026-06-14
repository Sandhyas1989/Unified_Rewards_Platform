# -*- coding: utf-8 -*-
"""Generates two 'as-built' architecture diagrams (PNG) for the Unified Rewards Platform:
   1. as_built_logical.png      — layered logical architecture (frontend MF + .NET modular monolith)
   2. as_built_deployment.png   — local (zero-install) vs Microsoft Azure deployment mapping
Self-contained (matplotlib only, no system binaries)."""

import os
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch

OUT = os.path.dirname(os.path.abspath(__file__))

INK = "#0f172a"
C = {
    "front":  ("#EEF2FF", "#6366F1"),
    "shell":  ("#C7D2FE", "#4338CA"),
    "remote": ("#E0E7FF", "#6366F1"),
    "api":    ("#DBEAFE", "#2563EB"),
    "app":    ("#E0E7FF", "#4F46E5"),
    "module": ("#FFFFFF", "#4F46E5"),
    "domain": ("#FCE7F3", "#DB2777"),
    "infra":  ("#DCFCE7", "#16A34A"),
    "seam":   ("#FEF3C7", "#D97706"),
    "data":   ("#E2E8F0", "#475569"),
    "local":  ("#DBEAFE", "#2563EB"),
    "azure":  ("#D1FAE5", "#059669"),
    "band":   ("#F8FAFC", "#CBD5E1"),
}


def box(ax, x, y, w, h, text, kind="module", fs=9, bold=False, align="center", round=0.12):
    fill, edge = C[kind]
    p = FancyBboxPatch((x, y), w, h, boxstyle=f"round,pad=0.02,rounding_size={round}",
                       linewidth=1.4, edgecolor=edge, facecolor=fill, mutation_aspect=1)
    ax.add_patch(p)
    ha = {"center": "center", "left": "left"}[align]
    tx = x + w / 2 if align == "center" else x + 0.6
    ax.text(tx, y + h / 2, text, ha=ha, va="center", fontsize=fs,
            color=INK, weight="bold" if bold else "normal", wrap=True)


def band(ax, x, y, w, h, label):
    fill, edge = C["band"]
    p = FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02,rounding_size=0.2",
                       linewidth=1.6, edgecolor=edge, facecolor=fill, linestyle="--")
    ax.add_patch(p)
    ax.text(x + 0.8, y + h - 0.9, label, ha="left", va="top", fontsize=10.5,
            color="#334155", weight="bold")


def arrow(ax, x1, y1, x2, y2, text=None, color="#475569", fs=8.5):
    ax.annotate("", xy=(x2, y2), xytext=(x1, y1),
                arrowprops=dict(arrowstyle="-|>", color=color, lw=1.6, shrinkA=2, shrinkB=2))
    if text:
        ax.text((x1 + x2) / 2, (y1 + y2) / 2, text, ha="center", va="center",
                fontsize=fs, color=color, backgroundcolor="white")


def new_ax(w, h):
    fig, ax = plt.subplots(figsize=(w, h))
    ax.set_xlim(0, 100); ax.set_ylim(0, 100); ax.axis("off")
    return fig, ax


# =====================================================================
# DIAGRAM 1 — LOGICAL ARCHITECTURE
# =====================================================================
fig, ax = new_ax(13.5, 10.5)
ax.text(50, 98, "Unified Rewards Platform — As-Built Logical Architecture",
        ha="center", va="center", fontsize=15, weight="bold", color=INK)

# ---- Frontend band ----
band(ax, 4, 79, 92, 17, "Browser  ·  React 18 + Webpack 5 Module Federation (micro-frontends)")
box(ax, 38, 89.3, 24, 5, "Shell Host  :3000\nlogin · auth · routing · layout", "shell", fs=8.5, bold=True)
remotes = [("Employee\nPortal  :3001"), ("Manager\nPortal  :3002"), ("HR\nPortal  :3003"), ("Finance\nPortal  :3004")]
rx = 6
for i, r in enumerate(remotes):
    x = 6 + i * 22.5
    box(ax, x, 80.5, 19, 5, r, "remote", fs=8.5)
    arrow(ax, x + 9.5, 85.5, 48 + (i - 1.5) * 2, 89.3)  # remote -> shell (federated)

arrow(ax, 50, 79, 50, 73.2, "HTTPS  ·  JWT Bearer token  ·  CORS", fs=9)

# ---- Backend band ----
band(ax, 4, 20, 92, 52, ".NET 8 Web API  ·  Clean Architecture (modular monolith)  —  dependencies point inward")

# API layer
box(ax, 6, 64.5, 88, 6, "API layer   —   Controllers (api/v1)  ·  JWT authentication + RBAC  ·  Serilog  ·  Exception→HTTP middleware  ·  Audit pipeline",
    "api", fs=8.5, bold=True)

# Application layer + 7 modules
box(ax, 6, 45.5, 88, 17.5, "", "app", fs=8.5)
ax.text(8, 61.5, "Application layer   —   CQRS via MediatR  ·  FluentValidation  ·  pipeline behaviours (Logging / Validation / Audit)",
        ha="left", va="center", fontsize=8.5, weight="bold", color=INK)
modules = ["User\nManagement", "Benefits", "Compen-\nsation\n(NRules)", "Claims &\nDocuments\n(state m/c)",
           "Payroll\n(Polly +\nasync)", "Promotions\n(events)", "Reporting\n& Audit\n(LINQ/xlsx)"]
mw, gap, mx0 = 11.2, 1.3, 7
for i, m in enumerate(modules):
    box(ax, mx0 + i * (mw + gap), 46.5, mw, 11.5, m, "module", fs=7.6, bold=True)

# Domain layer
box(ax, 6, 38, 88, 6, "Domain layer   —   Entities & aggregates  ·  business invariants  ·  state machines (Claims, Promotions)  ·  enums",
    "domain", fs=8.5, bold=True)

# Infrastructure layer
box(ax, 6, 21, 88, 15, "", "infra", fs=8.5)
ax.text(8, 34.5, "Infrastructure layer   —   EF Core 8  ·  NRules engine  ·  Polly resilience  ·  background worker  ·  seam implementations",
        ha="left", va="center", fontsize=8.5, weight="bold", color=INK)
seams = ["IEmailService", "IPayrollService", "IFileStorage", "IOcrEngine", "IEventBus", "ICompensation\nCalculator"]
sw = 13.5
sx0 = 7
for i, s in enumerate(seams):
    box(ax, sx0 + i * (sw + 0.6), 22, sw, 9.5, s + "\n(seam)", "seam", fs=7.6)

arrow(ax, 50, 20, 50, 16.2, "implemented by", fs=8.5)

# ---- External resources band ----
band(ax, 4, 2, 92, 13.5, "External resources (each hidden behind a seam — swappable local ↔ Azure; see deployment view)")
ext = ["Database\n(SQLite / Azure SQL)", "E-mail\n(smtp4dev / ACS)", "Payroll system\n(mock / REST)",
       "File storage\n(local FS / Blob)", "OCR\n(Tesseract / Doc Intel.)", "Messaging\n(Channel / Service Bus)"]
ew = 14
ex0 = 5.5
for i, e in enumerate(ext):
    box(ax, ex0 + i * (ew + 1), 4, ew, 8, e, "data", fs=7.6)

fig.tight_layout()
fig.savefig(os.path.join(OUT, "as_built_logical.png"), dpi=150, bbox_inches="tight")
plt.close(fig)

# =====================================================================
# DIAGRAM 2 — LOCAL vs AZURE DEPLOYMENT
# =====================================================================
fig, ax = new_ax(12.5, 10.5)
ax.text(50, 98, "Unified Rewards Platform — Local (zero-install) vs Microsoft Azure",
        ha="center", va="center", fontsize=15, weight="bold", color=INK)
ax.text(25, 93.5, "Local  (developer laptop)", ha="center", fontsize=12, weight="bold", color="#1D4ED8")
ax.text(75, 93.5, "Microsoft Azure  (production)", ha="center", fontsize=12, weight="bold", color="#047857")
ax.text(50, 93.5, "swap by\nconfiguration", ha="center", fontsize=8, color="#64748b", style="italic")

rows = [
    ("Frontend hosting", "Webpack dev servers (:3000–:3004)", "Azure Static Web Apps"),
    ("Backend API hosting", "Kestrel (dotnet run)", "App Service / Container Apps"),
    ("Database", "SQLite (single file)", "Azure SQL Database"),
    ("Authentication", "Self-issued JWT", "Microsoft Entra ID (SSO)"),
    ("Secrets & keys", "Local config files", "Azure Key Vault"),
    ("E-mail", "smtp4dev / console log", "Azure Communication Services"),
    ("File / receipt storage", "Local file system", "Azure Blob Storage"),
    ("OCR (read receipts)", "Tesseract / stub", "Azure AI Document Intelligence"),
    ("Async messaging / events", "In-memory Channel + MediatR", "Azure Service Bus"),
    ("Background processing", "Hosted BackgroundService", "Azure Functions / CA Job"),
    ("Logging & monitoring", "Serilog → console", "Application Insights"),
]

top = 90
rh = 7.4
for i, (concern, local, azure) in enumerate(rows):
    y = top - (i + 1) * rh
    box(ax, 3, y + 0.6, 30, rh - 1.4, local, "local", fs=8)
    box(ax, 67, y + 0.6, 30, rh - 1.4, azure, "azure", fs=8)
    arrow(ax, 33.5, y + rh / 2, 66.5, y + rh / 2, color="#94a3b8")
    ax.text(50, y + rh / 2, concern, ha="center", va="center", fontsize=8, color="#334155",
            weight="bold", bbox=dict(boxstyle="round,pad=0.25", fc="white", ec="none"))

ax.text(50, 3.2, "The seven business modules and all business logic are unchanged between the two environments —\nonly the implementation behind each seam differs.",
        ha="center", va="center", fontsize=9, color="#475569", style="italic")

fig.tight_layout()
fig.savefig(os.path.join(OUT, "as_built_deployment.png"), dpi=150, bbox_inches="tight")
plt.close(fig)

print("SAVED:", os.path.join(OUT, "as_built_logical.png"))
print("SAVED:", os.path.join(OUT, "as_built_deployment.png"))
