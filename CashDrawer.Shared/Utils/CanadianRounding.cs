using System;

namespace CashDrawer.Shared.Utils
{
    /// <summary>
    /// Canadian penny rounding rules (cash transactions)
    /// Since pennies were eliminated in 2013, cash totals are rounded to nearest $0.05
    /// </summary>
    public static class CanadianRounding
    {
        /// <summary>
        /// Round to nearest nickel (5 cents) using Canadian rounding rules
        /// </summary>
        /// <param name="amount">Amount to round</param>
        /// <returns>Rounded amount</returns>
        public static decimal RoundToNickel(decimal amount)
        {
            // Get the cents portion
            var cents = (amount - Math.Floor(amount)) * 100;
            var dollars = Math.Floor(amount);
            
            // Round to nearest 5 cents
            // 0.01, 0.02 → 0.00
            // 0.03, 0.04, 0.05, 0.06, 0.07 → 0.05
            // 0.08, 0.09, 0.10, 0.11, 0.12 → 0.10
            // etc.
            
            var roundedCents = Math.Round(cents / 5) * 5;
            
            return dollars + (roundedCents / 100);
        }

        /// <summary>
        /// Calculate change with Canadian rounding
        /// </summary>
        /// <param name="amountPaid">Amount customer paid</param>
        /// <param name="total">Total amount owed</param>
        /// <returns>Change to give (rounded to nickel)</returns>
        public static decimal CalculateChange(decimal amountPaid, decimal total)
        {
            var change = amountPaid - total;
            return RoundToNickel(change);
        }

        /// <summary>
        /// Get rounding adjustment (how much was rounded)
        /// </summary>
        /// <param name="original">Original amount</param>
        /// <returns>Adjustment amount (can be positive or negative)</returns>
        public static decimal GetRoundingAdjustment(decimal original)
        {
            return RoundToNickel(original) - original;
        }

        /// <summary>
        /// Format amount for display with rounding indicator
        /// </summary>
        /// <param name="original">Original amount</param>
        /// <returns>Formatted string showing rounding</returns>
        public static string FormatWithRounding(decimal original)
        {
            var rounded = RoundToNickel(original);
            var adjustment = rounded - original;
            
            if (adjustment == 0)
            {
                return $"${rounded:0.00}";
            }
            else if (adjustment > 0)
            {
                return $"${rounded:0.00} (rounded up ${Math.Abs(adjustment):0.00})";
            }
            else
            {
                return $"${rounded:0.00} (rounded down ${Math.Abs(adjustment):0.00})";
            }
        }
    }
}
