namespace Identity.Domain.Constants
{
    public static class OtpPolicy
    {
        public const int Length = 6;
        public const int ExpiryMinutes = 10;
        public const int ResetTokenExpiryMinutes = 15;

        // TODO: remove before production
        public const string StaticOtp = "123456";
    }
}
