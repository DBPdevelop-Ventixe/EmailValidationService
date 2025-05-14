using System.ComponentModel.DataAnnotations;

namespace WebApi.Models;

public class SendVerificationCodeRequest
{
    [Required]
    public string Email { get; set; } = null!;
}
