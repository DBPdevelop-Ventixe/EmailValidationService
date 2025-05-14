namespace WebApi.Models;

public class VerificationServiceResult
{
    public bool Succeded { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}
