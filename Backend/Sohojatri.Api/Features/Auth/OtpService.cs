using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;

namespace Sohojatri.Api.Features.Auth;

public interface IOtpService
{
    Task IssueCodeAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task<bool> VerifyCodeAsync(string mobileNumber, string code, CancellationToken cancellationToken = default);
}

public class OtpService(IDistributedCache cache) : IOtpService
{
    private static readonly TimeSpan Expiry = TimeSpan.FromMinutes(3);

    public async Task IssueCodeAsync(string mobileNumber, CancellationToken cancellationToken = default)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        await cache.SetStringAsync(Key(mobileNumber), code, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Expiry
        }, cancellationToken);
    }

    public async Task<bool> VerifyCodeAsync(string mobileNumber, string code, CancellationToken cancellationToken = default)
    {
        var cachedCode = await cache.GetStringAsync(Key(mobileNumber), cancellationToken);
        if (cachedCode is null || cachedCode != code)
        {
            return false;
        }

        await cache.RemoveAsync(Key(mobileNumber), cancellationToken);
        return true;
    }

    private static string Key(string mobileNumber) => $"otp:{mobileNumber}";
}
