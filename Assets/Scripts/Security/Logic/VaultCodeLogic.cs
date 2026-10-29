namespace HighStakes.Security
{
    public enum VaultAttempt { Incomplete, Wrong, Lockdown, Open }

    /// <summary>
    /// Pure rules for the vault keypad: a full wrong code counts as a failure, and the last allowed failure trips a
    /// lockdown and starts the count again. A short entry is not a failure. No Unity types, covered by EditMode tests.
    /// </summary>
    public static class VaultCodeLogic
    {
        public static VaultAttempt Submit(string entry, string code, ref int failures, int maxFailures)
        {
            entry ??= "";
            if (string.IsNullOrEmpty(code) || entry.Length < code.Length) return VaultAttempt.Incomplete;
            if (entry == code)
            {
                failures = 0;
                return VaultAttempt.Open;
            }
            failures++;
            if (failures < maxFailures) return VaultAttempt.Wrong;
            failures = 0;
            return VaultAttempt.Lockdown;
        }

        public static int AttemptsLeft(int failures, int maxFailures) => failures >= maxFailures ? 0 : maxFailures - failures;
    }
}
