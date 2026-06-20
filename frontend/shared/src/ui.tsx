import React from 'react';

export function Card({ title, children, actions }: { title?: string; children: React.ReactNode; actions?: React.ReactNode }) {
  return (
    <section className="urp-card">
      {(title || actions) && (
        <header className="urp-card__head">
          {title && <h3>{title}</h3>}
          {actions && <div className="urp-card__actions">{actions}</div>}
        </header>
      )}
      <div className="urp-card__body">{children}</div>
    </section>
  );
}

export function Button({
  children,
  variant = 'primary',
  ...rest
}: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: 'primary' | 'ghost' | 'danger' }) {
  return (
    <button className={`urp-btn urp-btn--${variant}`} {...rest}>
      {children}
    </button>
  );
}

export function Badge({ children, tone = 'neutral' }: { children: React.ReactNode; tone?: 'neutral' | 'good' | 'warn' | 'bad' }) {
  return <span className={`urp-badge urp-badge--${tone}`}>{children}</span>;
}

export function Field({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <label className="urp-field">
      <span className="urp-field__label">{label}</span>
      {children}
    </label>
  );
}

export function Table<T>({ columns, rows, empty = 'No records.' }: {
  columns: { header: string; render: (row: T) => React.ReactNode }[];
  rows: T[];
  empty?: string;
}) {
  if (rows.length === 0) {
    return <p className="urp-empty">{empty}</p>;
  }
  return (
    <table className="urp-table">
      <thead>
        <tr>{columns.map((c, i) => <th key={i}>{c.header}</th>)}</tr>
      </thead>
      <tbody>
        {rows.map((row, ri) => (
          <tr key={ri}>{columns.map((c, ci) => <td key={ci}>{c.render(row)}</td>)}</tr>
        ))}
      </tbody>
    </table>
  );
}

export function Banner({ kind, message }: { kind: 'error' | 'success'; message: string }) {
  if (!message) return null;
  return <div className={`urp-banner urp-banner--${kind}`}>{message}</div>;
}

export function money(n: number, currencyCode = 'INR'): string {
  const locale = currencyCode === 'INR' ? 'en-IN' : 'en-US';
  return new Intl.NumberFormat(locale, { style: 'currency', currency: currencyCode, maximumFractionDigits: 0 }).format(n);
}
