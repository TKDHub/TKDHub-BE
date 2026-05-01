namespace Identity.Infrastructure.Services
{
    internal static class OtpEmailTemplate
    {
        public static string Build(string otp, int expiryMinutes) => $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
              <meta charset="UTF-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
              <title>Your OTP Code</title>
            </head>
            <body style="margin:0;padding:0;background:#f4f6f8;font-family:'Segoe UI',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f8;padding:40px 0;">
                <tr>
                  <td align="center">
                    <table width="520" cellpadding="0" cellspacing="0" style="background:#ffffff;border-radius:12px;overflow:hidden;box-shadow:0 2px 12px rgba(0,0,0,0.08);">

                      <!-- Header -->
                      <tr>
                        <td style="background:#1a1a2e;padding:32px 40px;text-align:center;">
                          <h1 style="margin:0;color:#ffffff;font-size:24px;font-weight:700;letter-spacing:1px;">TKDHub</h1>
                        </td>
                      </tr>

                      <!-- Body -->
                      <tr>
                        <td style="padding:40px 40px 24px;">
                          <h2 style="margin:0 0 12px;color:#1a1a2e;font-size:20px;">Verification Code</h2>
                          <p style="margin:0 0 28px;color:#555;font-size:15px;line-height:1.6;">
                            Use the code below to verify your identity. Do not share this code with anyone.
                          </p>

                          <!-- OTP Box -->
                          <div style="background:#f0f4ff;border:2px dashed #4f6ef7;border-radius:10px;padding:28px;text-align:center;margin-bottom:28px;">
                            <span style="font-size:42px;font-weight:800;letter-spacing:12px;color:#1a1a2e;font-family:'Courier New',monospace;">{otp}</span>
                          </div>

                          <p style="margin:0 0 8px;color:#888;font-size:13px;text-align:center;">
                            This code expires in <strong style="color:#e74c3c;">{expiryMinutes} minutes</strong>.
                          </p>
                          <p style="margin:0;color:#888;font-size:13px;text-align:center;">
                            If you did not request this, please ignore this email.
                          </p>
                        </td>
                      </tr>

                      <!-- Footer -->
                      <tr>
                        <td style="background:#f9f9f9;padding:20px 40px;text-align:center;border-top:1px solid #eee;">
                          <p style="margin:0;color:#aaa;font-size:12px;">
                            &copy; {DateTime.UtcNow.Year} TKDHub. All rights reserved.
                          </p>
                        </td>
                      </tr>

                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
