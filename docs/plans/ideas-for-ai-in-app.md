# AI Feature Ideas for Budget Tracker

## Feature Summaries

**Feature: Transaction Categorisation**
Why: saves manual tagging of hundreds of imported transactions.
Input: description, amount, merchant name, date.
Output: `{ category: "Groceries", confidence: 0.94 }`

---

**Feature: Anomaly & Fraud Detection**
Why: spots unusual spending that a user would likely miss in a long list.
Input: last 90 days of transactions for a user, new transaction.
Output: `{ flagged: true, reason: "3x your usual spend at this merchant", severity: "medium" }`

---

**Feature: Monthly Budget Forecast**
Why: predicts end-of-month spend so users can adjust behaviour mid-month.
Input: transactions for the current month to date, 3–6 months of historical categorised transactions, user-defined budget targets per category (optional).
Output: average monthly surplus, surplus range, projected overspend by category, mid-month alerts with suggested daily limits, irregular month warnings.

**Detail:** AI establishes a baseline from historical spend, anchors the forecast on known recurring payments, and projects variable spend using current month's spend-to-date plus historical daily rates. Alerts are surfaced mid-month (e.g. on the 12th) so the user has time to act. Certain months (December, summer) are identified as reliably expensive and the forecast adjusts accordingly.

---

**Feature: Natural Language Transaction Search**
Why: users think in plain English ("coffee last week"), not SQL filters.
Input: query string, user's transaction history as embeddings (pgvector).
Output: ranked list of matching transactions with relevance scores.

---

**Feature: Personalised Saving Recommendations**
Why: generic advice ignores actual spending patterns; AI makes it specific.
Input: 3–6 months of categorised transactions, stated savings goal.
Output: `{ recommendations: [{ action: "Cancel 4 unused subscriptions", estimated_saving: 47.00 }] }`

---

**Feature: CSV Format Auto-Detection**
Why: banks export wildly different column layouts; AI removes the manual mapping step.
Input: first 5 rows of uploaded CSV.
Output: `{ date_col: 2, description_col: 4, amount_col: 6, format: "Barclays", confidence: 0.91 }`

---

**Feature: Recurring Payment Detection**
Why: subscriptions creep up unnoticed; AI surfaces them without users having to hunt.
Input: 6 months of transactions for a user.
Output: `{ subscriptions: [{ merchant: "Netflix", amount: 15.99, frequency: "monthly", last_charged: "2026-04-01" }] }`

---

**Feature: Split Transaction Suggestions**
Why: a single supermarket shop often covers groceries, toiletries, and alcohol — different budget buckets.
Input: transaction description, amount, merchant category, user's historical splits at that merchant.
Output: suggested split by category with confidence scores per line.

**Detail:** AI flags transactions likely to span multiple categories based on merchant type and transaction size. Splits are suggestions the user confirms or adjusts — corrections feed back to improve future suggestions. Accuracy improves significantly when a receipt is available (photo OCR, forwarded email, or PDF upload), lifting confidence to near 1.0 by replacing inference with actual line items. Key merchants: supermarkets, pharmacies, Amazon, Costco.

---

**Feature: Financial Health Score**
Why: a single number gives users an at-a-glance signal without reading every chart.
Input: income, fixed costs, savings rate, debt repayments, spending volatility over 90 days.
Output: `{ score: 72, rating: "Good", top_risk: "Low emergency fund relative to monthly spend" }`

---

**Feature: Smart Import Deduplication**
Why: re-importing an overlapping CSV silently creates duplicate transactions.
Input: incoming transactions from CSV + existing transactions in the same date range.
Output: `{ duplicates: [{ incoming_id: "...", matched_id: "...", confidence: 0.98 }], safe_to_import: 43 }`

---

**Feature: Essential vs Non-Essential Breakdown with What-If Planner**
Why: AI classifies spending into essential and discretionary so users get a concrete survival budget and runway number without manual categorisation.
Input: 3–6 months of transactions, savings balance, scenario description.
Output: essential/discretionary monthly totals, runway in months, quick-win cuts ranked by impact.

**Detail:** Transactions are classified as essential (rent, utilities, groceries, insurance) or discretionary (dining, streaming, clothing) with user override available. The user enters a what-if scenario (e.g. "I lose my job", "income drops 40%") and the app calculates how many months their savings covers essential costs only. Quick wins are surfaced ranked by how much they extend the runway.

---

**Feature: Monthly Surplus Calculator with Savings Allocator**
Why: AI derives true disposable income from actual spend patterns and allocates it across goals in priority order.
Input: savings balance, up to three goals each with an optional target date (e.g. *Build emergency fund by Dec 2026, Save for holiday, Invest*).
Output: average monthly surplus, surplus range, recommended allocation split across the selected goals ranked by urgency and priority, months to reach each goal at the suggested contribution rate.

**Detail:** Income is inferred from recurring credits in the transaction feed — no manual declaration needed. Essential and discretionary spend flows in from the breakdown feature. Goals with a deadline are weighted by monthly contribution required to hit them in time; goals without a date fill remaining surplus. An emergency fund gate checks whether 3–6 months of essential costs are covered before recommending investment.

---

**Feature: Make Your Money Work Harder**
Why: AI ranks specific financial opportunities by actual impact against the user's own numbers, surfacing non-obvious moves like clearing high-interest debt before saving.
Input: surplus amount, savings balance, debts and interest rates, pension contribution rate, employer match rate, recurring essential costs.
Output: prioritised list of opportunities each with action, estimated annual gain, and effort level, plus total potential annual gain.

**Detail:** Opportunities include moving idle cash to high-interest accounts, clearing high-APR debt before saving, maximising employer pension match, and switching recurring costs to cheaper tariffs. Opportunities are ranked by estimated annual gain. A disclaimer is shown making clear these are data-driven suggestions, not regulated financial advice.

---

## Feature Dependencies

Several features build on each other and share an underlying data pipeline:

```
CSV Import
    └── CSV Format Auto-Detection
    └── Smart Import Deduplication

Transaction Categorisation
    └── Split Transaction Suggestions (receipt ingestion improves accuracy)
    └── Recurring Payment Detection
    └── Anomaly & Fraud Detection

Essential vs Non-Essential Breakdown
    └── Monthly Surplus Calculator with Savings Allocator
        └── Make Your Money Work Harder

Monthly Budget Forecast (parallel track — uses categorisation)
Natural Language Transaction Search (parallel track — uses embeddings)
Financial Health Score (parallel track — aggregates across all features)
```

Getting categorisation right is the highest-leverage investment — it is the data quality layer that every downstream feature depends on.
