using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers;
[Route("api/[controller]")]
[ApiController]
public class VerificationController(IVerificationService verificationService) : ControllerBase
{
    private readonly IVerificationService _verificationService = verificationService;

    [HttpPost("send")]
    public async Task<IActionResult> SendVerificationCode([FromBody] SendVerificationCodeRequest request)
    {
        if(!ModelState.IsValid)
            return BadRequest(new { Error = "Email address is required"} );

        var result = await _verificationService.SendVerificationCodeAsync(request);
        if (result.Succeded)
            return Ok(result);
        return StatusCode(500, result.Error); // Internal Server Error
    }

    [HttpPost("verify")]
    public IActionResult VerifyVerificationCode([FromBody] VerifyVerificationCodeRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { Error = "Email address and verification code are required" });

        var result = _verificationService.VerifyVerificationCode(request);
        if (result.Succeded)
            return Ok(result);
        return StatusCode(500, result.Error); // Internal Server Error
    }
}
