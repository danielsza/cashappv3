"""
Canadian Penny Rounding Test Script
Demonstrates the penny rounding rules used in the Cash App
"""

import math

def canadian_penny_rounding(amount):
    """
    Apply Canadian penny rounding rules for cash transactions.
    
    Fair Rounding Rule:
    * Ends in .01 or .02: Rounds down to .00
    * Ends in .03 or .04: Rounds up to .05
    * Ends in .06 or .07: Rounds down to .05
    * Ends in .08 or .09: Rounds up to .10
    """
    # Get the cents portion
    cents = round((amount - math.floor(amount)) * 100)
    dollars = math.floor(amount)
    
    # Apply rounding rules
    if cents in [1, 2]:
        rounded_cents = 0
    elif cents in [3, 4]:
        rounded_cents = 5
    elif cents in [6, 7]:
        rounded_cents = 5
    elif cents in [8, 9]:
        rounded_cents = 10
    else:
        # 0, 5, or values that round to 10+ stay as is
        rounded_cents = cents
    
    # Handle the case where cents rounds to 10
    if rounded_cents >= 10:
        return dollars + (rounded_cents / 100.0)
    else:
        return dollars + (rounded_cents / 100.0)

def test_penny_rounding():
    """Test all penny rounding scenarios"""
    
    print("=" * 70)
    print("Canadian Penny Rounding Test")
    print("=" * 70)
    print()
    
    # Test cases for each cent value
    test_cases = [
        # (description, amount, expected_rounded)
        ("Ends in .00 (already rounded)", 10.00, 10.00),
        ("Ends in .01 (rounds to .00)", 10.01, 10.00),
        ("Ends in .02 (rounds to .00)", 10.02, 10.00),
        ("Ends in .03 (rounds to .05)", 10.03, 10.05),
        ("Ends in .04 (rounds to .05)", 10.04, 10.05),
        ("Ends in .05 (already rounded)", 10.05, 10.05),
        ("Ends in .06 (rounds to .05)", 10.06, 10.05),
        ("Ends in .07 (rounds to .05)", 10.07, 10.05),
        ("Ends in .08 (rounds to .10)", 10.08, 10.10),
        ("Ends in .09 (rounds to .10)", 10.09, 10.10),
        ("Ends in .10 (already rounded)", 10.10, 10.10),
    ]
    
    print("Basic Rounding Tests:")
    print("-" * 70)
    
    all_passed = True
    for description, amount, expected in test_cases:
        result = canadian_penny_rounding(amount)
        status = "✓ PASS" if result == expected else "✗ FAIL"
        
        if result != expected:
            all_passed = False
        
        print(f"{status} | {description:<30} | ${amount:.2f} → ${result:.2f}")
    
    print()
    print("=" * 70)
    print()
    
    # Real-world transaction examples
    print("Real-World Transaction Examples:")
    print("-" * 70)
    
    transactions = [
        ("Small purchase", 2.37, 5.00),
        ("Coffee", 4.67, 10.00),
        ("Groceries", 23.48, 30.00),
        ("Auto parts", 45.53, 50.00),
        ("Large purchase", 123.91, 125.00),
    ]
    
    for description, total, cash_given in transactions:
        raw_change = cash_given - total
        rounded_change = canadian_penny_rounding(raw_change)
        difference = rounded_change - raw_change
        
        print(f"\n{description}:")
        print(f"  Total:          ${total:.2f}")
        print(f"  Cash Given:     ${cash_given:.2f}")
        print(f"  Change (exact): ${raw_change:.2f}")
        print(f"  Change (round): ${rounded_change:.2f}", end="")
        
        if difference > 0:
            print(f"  (customer gets ${abs(difference):.2f} more)")
        elif difference < 0:
            print(f"  (store keeps ${abs(difference):.2f})")
        else:
            print(f"  (no adjustment needed)")
    
    print()
    print("=" * 70)
    
    if all_passed:
        print("✓ All tests PASSED!")
    else:
        print("✗ Some tests FAILED!")
    
    print("=" * 70)
    print()
    
    # Interactive test
    print("Interactive Test:")
    print("-" * 70)
    print("Enter transaction details to see penny rounding in action.")
    print("(Press Ctrl+C to exit)")
    print()
    
    try:
        while True:
            try:
                total = float(input("Enter total amount: $"))
                cash_given = float(input("Enter cash given: $"))
                
                if cash_given < total:
                    print("Error: Cash given must be >= total!\n")
                    continue
                
                raw_change = cash_given - total
                rounded_change = canadian_penny_rounding(raw_change)
                
                print(f"\n  Exact change: ${raw_change:.2f}")
                print(f"  Rounded change: ${rounded_change:.2f}")
                
                difference = rounded_change - raw_change
                if difference > 0:
                    print(f"  → Customer receives ${abs(difference):.2f} extra")
                elif difference < 0:
                    print(f"  → Store keeps ${abs(difference):.2f}")
                else:
                    print(f"  → No adjustment (already rounded)")
                
                print()
                
            except ValueError:
                print("Invalid input! Please enter numbers only.\n")
    
    except KeyboardInterrupt:
        print("\n\nExiting...")

if __name__ == '__main__':
    test_penny_rounding()
