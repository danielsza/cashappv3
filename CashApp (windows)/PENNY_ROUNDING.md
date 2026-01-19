# Canadian Penny Rounding - Implementation Guide

## Overview

Since Canada eliminated the penny in 2013, cash transactions must follow fair rounding rules. This system automatically applies Canadian penny rounding to all change calculations.

## The Rules

When calculating change for **cash transactions**, the total is rounded to the nearest 5 cents:

| Last Digit | Rounds To | Example |
|------------|-----------|---------|
| .00        | .00       | $10.00 → $10.00 ✓ |
| .01        | .00       | $10.01 → $10.00 ⬇️ |
| .02        | .00       | $10.02 → $10.00 ⬇️ |
| .03        | .05       | $10.03 → $10.05 ⬆️ |
| .04        | .05       | $10.04 → $10.05 ⬆️ |
| .05        | .05       | $10.05 → $10.05 ✓ |
| .06        | .05       | $10.06 → $10.05 ⬇️ |
| .07        | .05       | $10.07 → $10.05 ⬇️ |
| .08        | .10       | $10.08 → $10.10 ⬆️ |
| .09        | .10       | $10.09 → $10.10 ⬆️ |
| .10        | .10       | $10.10 → $10.10 ✓ |

### Summary
- **.01 or .02** → Rounds **down** to **.00**
- **.03 or .04** → Rounds **up** to **.05**
- **.06 or .07** → Rounds **down** to **.05**
- **.08 or .09** → Rounds **up** to **.10**
- **.00 or .05** → **No change** (already rounded)

## How It Works in the App

### Automatic Calculation

When you enter transaction details:

1. **Enter Total**: Sale amount (e.g., $45.53)
2. **Enter IN**: Cash received (e.g., $50.00)
3. **OUT Calculates Automatically**: 
   - Raw change: $50.00 - $45.53 = $4.47
   - **Rounded change: $4.45** (applied automatically)

The app displays: `🍁 Canadian penny rounding applied` below the amount fields.

### Real-World Examples

#### Example 1: Customer Benefits
```
Total:    $12.37
Cash:     $20.00
Change:   $7.63 (exact) → $7.65 (rounded)
```
Customer receives **2¢ more**

#### Example 2: Store Benefits
```
Total:    $8.48
Cash:     $10.00
Change:   $1.52 (exact) → $1.50 (rounded)
```
Store keeps **2¢**

#### Example 3: No Adjustment
```
Total:    $15.45
Cash:     $20.00
Change:   $4.55 (exact) → $4.55 (rounded)
```
No adjustment needed (ends in .05)

## Implementation Details

### Code Location

The rounding function is in `CashClient.py`:

```python
def canadian_penny_rounding(self, amount):
    """Apply Canadian penny rounding rules"""
    cents = round((amount - math.floor(amount)) * 100)
    dollars = math.floor(amount)
    
    if cents in [1, 2]:
        rounded_cents = 0
    elif cents in [3, 4]:
        rounded_cents = 5
    elif cents in [6, 7]:
        rounded_cents = 5
    elif cents in [8, 9]:
        rounded_cents = 10
    else:
        rounded_cents = cents
    
    return dollars + (rounded_cents / 100.0)
```

### When Applied

Rounding is applied:
- ✅ Automatically when calculating change (IN - Total)
- ✅ Real-time as you type amounts
- ✅ Before sending to server for logging
- ✅ Displayed in OUT field

### When NOT Applied

Rounding is NOT applied:
- ❌ To credit/debit card transactions (exact amounts)
- ❌ To electronic payments (exact amounts)
- ❌ To the Total field (only to calculated change)

## Testing the Rounding

### Using the Test Script

Run the included test script:

```bash
python Test_Penny_Rounding.py
```

This will:
1. Run automated tests on all rounding scenarios
2. Show real-world transaction examples
3. Provide an interactive calculator

### Test Cases

The script tests all scenarios:

| Input  | Expected | Status |
|--------|----------|--------|
| $10.01 | $10.00   | ✓ PASS |
| $10.02 | $10.00   | ✓ PASS |
| $10.03 | $10.05   | ✓ PASS |
| $10.04 | $10.05   | ✓ PASS |
| $10.06 | $10.05   | ✓ PASS |
| $10.07 | $10.05   | ✓ PASS |
| $10.08 | $10.10   | ✓ PASS |
| $10.09 | $10.10   | ✓ PASS |

## Compliance

This implementation follows:

- **Royal Canadian Mint** guidelines
- **Canadian Revenue Agency** (CRA) requirements
- **Fair rounding** principles (balances out over multiple transactions)

## UI Indicator

The client displays a small indicator to show penny rounding is active:

```
Total:  [45.53]
IN:     [50.00]
Out:    [4.45]
🍁 Canadian penny rounding applied
```

This ensures staff are aware that rounding is being applied.

## Logging

All transactions are logged with the **rounded** amounts:

```
2025-01-19T10:30:45 | SERVER1 | daniel | Transaction | Invoice | 
Total: 45.53 | IN: 50.00 | OUT: 4.45
```

This ensures your records match what was actually dispensed as change.

## FAQ

### Q: Why do we need penny rounding?
**A**: Canada eliminated the penny in 2013. Cash transactions must round to the nearest 5 cents.

### Q: Does this apply to debit/credit cards?
**A**: No. Electronic payments use exact amounts. Penny rounding only applies to physical cash.

### Q: Is this legal?
**A**: Yes. This follows the official Royal Canadian Mint guidelines for fair rounding.

### Q: Does it favor customers or the store?
**A**: It balances out. Sometimes customers get more (e.g., .03→.05), sometimes the store keeps more (e.g., .02→.00).

### Q: Can I disable it?
**A**: Not recommended as it's required by law in Canada. However, you can modify the code if needed.

### Q: What if I'm not in Canada?
**A**: You can remove the rounding by commenting out the `canadian_penny_rounding()` call in `calculate_change()`.

## Statistics

Over many transactions, fair rounding creates a balanced effect:

| Rounding | Frequency | Customer | Store |
|----------|-----------|----------|-------|
| .01→.00  | 10%       | -1¢      | +1¢   |
| .02→.00  | 10%       | -2¢      | +2¢   |
| .03→.05  | 10%       | +2¢      | -2¢   |
| .04→.05  | 10%       | +1¢      | -1¢   |
| .06→.05  | 10%       | -1¢      | +1¢   |
| .07→.05  | 10%       | -2¢      | +2¢   |
| .08→.10  | 10%       | +2¢      | -2¢   |
| .09→.10  | 10%       | +1¢      | -1¢   |

**Net effect over 100 transactions**: Nearly zero difference for both parties.

## References

- [Royal Canadian Mint - Eliminating the Penny](https://www.mint.ca/en/about-us/publications)
- [Canadian Revenue Agency - Rounding Guidelines](https://www.canada.ca/en/revenue-agency.html)

---

**Implementation Date**: January 2025  
**Compliance**: Canadian Fair Rounding Rules  
**Status**: ✅ Active in all cash transactions
