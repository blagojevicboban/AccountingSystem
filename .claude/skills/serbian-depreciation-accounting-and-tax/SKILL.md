---
name: serbian-depreciation-accounting-and-tax
description: Instructions and business logic rules for calculating Serbian accounting depreciation (MRS 16), tax depreciation (Obrazac OA & Zakon o porezu na dobit), temporary tax differences (PB-1), and fixed asset rules in ERPiFinansije.
---

# Serbian Depreciation Accounting & Tax Workflow (ERPiFinansije)

This skill provides mandatory business logic rules, formulas, and code structure guidelines for fixed assets and depreciation in the `ERPiFinansije` codebase.

---

## 1. Accounting Depreciation (MRS 16 / MSFI za MSP)

### Depreciable Base & Residual Value
- **Formula**: $Osnovica = \max(0, NabavnaVrednost - RezidualnaVrednost)$
- **Depreciation limit**: Total accumulated depreciation (`IspravkaVrednosti`) cannot exceed $(NabavnaVrednost - RezidualnaVrednost)$.

---

## 2. Tax Depreciation (Obrazac OA & Zakon o Porezu na Dobit)

- **Scope**: Post-2019 acquisitions / activations are depreciated individually using linear method by tax group rates (Grupa I-V).
- **Temporary Tax Difference**: $PrivremenaRazlika = RacunovodstvenaAmortizacija - PoreskaAmortizacija$. Used for Form PB-1 (Poreski bilans).
