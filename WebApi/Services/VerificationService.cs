using Azure.Communication.Email;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using WebApi.Models;

namespace WebApi.Services;


public interface IVerificationService
{
    Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest reqest);
    Task SaveVerifyCodeAsync(SaveVerificationCodeRequest request);
    VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request);
}

public class VerificationService(IConfiguration configuration, EmailClient emailClient, IMemoryCache memoryCache) : IVerificationService
{
    private readonly IConfiguration _configuration = configuration;
    private readonly EmailClient _emailClient = emailClient;
    private readonly IMemoryCache _memoryCache = memoryCache;

    private static readonly Random _random = new();


    public async Task<VerificationServiceResult> SendVerificationCodeAsync(SendVerificationCodeRequest reqest)
    {
        try
        {
            if (reqest == null || string.IsNullOrWhiteSpace(reqest.Email))
                return new VerificationServiceResult
                {
                    Succeded = false,
                    Error = "Invalid request."
                };

            var verificationCode = _random.Next(10000, 99999).ToString();

            var subject = $"Ventixe Account Verification Code";
            var plainTextContent = @$"
            Hello

            Your verification code is {verificationCode}.
            Please use this code to verify your email address.
            This code is valid for 5 minutes.

            Alternatively, you can use the following link to verify your email address:
            https://localhost:5001/api/verify?email={reqest.Email}&code={verificationCode}

            If you did not request this code, please ignore this email.

            Thank you for using our service.
            Best regards, DBP Develop
            ";

            var htmlContent = $@"
                <!DOCTYPE html>
                <html lang='en'>
                <head>
                  <meta charset='UTF-8'>
                  <title>Ventixe verification code</title>
                  <link href='https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&display=swap' rel='stylesheet'>
                </head>
                <body style='margin: 0; padding: 0; font-family: Inter, sans-serif; background-color: #ffffff; color: #1E1E20;'>

                  <div style='width: 100%; margin: 0 auto; max-width: 620px'>

                    <div style='margin-bottom: 1rem; padding: 3rem; gap: 1rem; background: radial-gradient(circle at 50% 50%, #f26cf9c0 0%, #ffffff 80%);'>
                        <h1 style='text-align: center;'>
                          <img src='https://privat.bahnhof.se/wb632954/WIN24/Assignment6/ventixelogo.png' alt='Ventixe Logo' style='width: 36px; height: 36px;'> Ventixe
                        </h>
                    </div>

                    <div style='max-width: 600px; margin: 0 auto; background-color: #F7F7F7; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);'>
                      <h2 style='font-size: 24px; margin-bottom: 10px;'>Welcome to Ventixe!</h2>
                      <p style='font-size: 16px; line-height: 1.5;'>Thank you for signing up! To complete your registration, please verify your email address by entering the code below:</p>
                      
                      <h3 style='font-size: 20px; margin-top: 20px;'>Your verification code:</h3>
                      <h1 style='text-align: center; font-size: 36px; font-weight: bold; color: #F26CF9;'>{verificationCode}</h1>

                      <p style='font-size: 16px; line-height: 1.5;'>If you did not request this email, please ignore it.</p>
                      <p style='font-size: 16px; line-height: 1.5;'>Best regards<br><span style='font-style: italic;'>DBP Develop team</span></p>
                    </div>
                  </div>

                </body>
                </html>
            ";

            var emailMessage = new EmailMessage(
                senderAddress: _configuration["ACS:SenderAddress"],
                recipients: new EmailRecipients([new(reqest.Email)]),
                content: new EmailContent(subject)
                {
                    PlainText = plainTextContent,
                    Html = htmlContent
                });

            var emailSendOperation = await _emailClient.SendAsync(Azure.WaitUntil.Started, emailMessage);

            // Store the verification code in memory cache
            await SaveVerifyCodeAsync(new SaveVerificationCodeRequest
            {
                Email = reqest.Email,
                Code = verificationCode,
                ValidFor = TimeSpan.FromMinutes(5)
            });

            return new VerificationServiceResult
            {
                Succeded = true,
                Message = "Verification code sent successfully."
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Email send operation did not complete successfully. {ex}");
            return new VerificationServiceResult
            {
                Succeded = false,
                Error = ex.Message
            };
        }
    }

    public Task SaveVerifyCodeAsync(SaveVerificationCodeRequest request)
    {
        _memoryCache.Set(request.Email, request.Code, request.ValidFor);
        return Task.CompletedTask;
    }

    public VerificationServiceResult VerifyVerificationCode(VerifyVerificationCodeRequest request)
    {
        var key = request.Email.ToLowerInvariant();

        // Check if the code is in the cache
        if (_memoryCache.TryGetValue(key, out string? storedCode))
        {
            // Check if the code is valid
            var isValid = storedCode == request.Code;
            if (isValid)
            {
                _memoryCache.Remove(key);
                return new VerificationServiceResult
                {
                    Succeded = true,
                    Message = "Verification code is valid."
                };
            }
        }
        // If the code is not valid or expired, return an error
        return new VerificationServiceResult
        {
            Succeded = false,
            Error = "Verification code is invalid or expired."
        };
    }
}
