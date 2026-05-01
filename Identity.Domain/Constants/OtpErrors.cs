using Shared.Domain.Primitives;

namespace Identity.Domain.Constants
{
    public static class OtpErrors
    {
        public static readonly Error InvalidOrExpired = new(
            "Otp.InvalidOrExpired",
            "OTP is invalid or has expired");

        public static readonly Error NotVerified = new(
            "Otp.NotVerified",
            "OTP has not been verified. Please verify your OTP first");
    }
}
