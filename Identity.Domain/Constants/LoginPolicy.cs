namespace Identity.Domain.Constants
{
    /// <summary>
    /// Login security policy constants.
    /// </summary>
    public static class LoginPolicy
    {
        /// <summary>Number of failed attempts before the account is locked out.</summary>
        public const int MaxFailedAttempts = 5;

        /// <summary>Duration in minutes for which the account remains locked after MaxFailedAttempts.</summary>
        public const int LockoutDurationMinutes = 30;
    }
}
