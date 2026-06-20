using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using UnifiedRewards.Messaging;
using UnifiedRewards.ReimbursementWorkflow.Persistence;

namespace UnifiedRewards.ReimbursementWorkflow.IntegrationTests;

static class TokenFactory
{
    internal const string DefaultTenant = "11111111-1111-1111-1111-111111111111";

    private const string PrivKey =
        "<RSAKeyValue><Modulus>y4rFl2A+gXUJDN4zbNUChU3GxTtUKJBOK2qU8wMKui52S4tJzbxvjji5ze/4o1yN+gD67U1gJdOnK3uIAh936lgN78B/YUtN7ug3n04DcyJhVXehLJjWmlMJdwLnhHdiel8ELZYJdBODoRJHg8CVNRbmiOGco4LX1sjolQIraA9cFkcB2A2/ZGY+f8xj04kIVtqk90QpUrNoYIgk+sx6CXf40M1k9UrqkFQmH7Z/cH0Su34SeJuzW7wTAIpBTj9TK9eXSIXwi0EHBTVOk2AfwApaNF+5Le7UEKrkxHp8X5IiE9270jcvrtA2wCArEXBptjaXpofJYmoJYwNrzrN3Kw==</Modulus><Exponent>AQAB</Exponent><P>6O8TvOQEAfC/9P2vO6wNOLV6OKexDmS/A0Zp0/Pv20waepK2KEqBh/b/IINWWL50nrCgJqZVN7HGcNnpIh3FZJbpErm6+3xpiMN8lCqAnj/IJY9jWmI7dchtUDpMbG4Y+YUk1NTgJANrKvwhtlQ9dlpJbyqcKhB0zBjN8COUCXk=</P><Q>37KbJKju92+MZxF7jFNQGU2lPrFJHtJNXKpqY0V1i67BigBrQIPYcDU5v+08HHDnjvsztPU2SccYEoVLW8nmFET/IN0BYyy1e35Zzs3cP5OK6Ejq56+v50NElrvtlBsJUGjw7EvMaYWICrnt5CboVW8jxeZO1MMNKLKx4lSiQMM=</Q><DP>0Q55tYcjvaYYckYYtsKlHydpaV2/v/5VnRfeNVdzB2wXO7le+WxdMu1QbxrRLVbKDf7Rzb5tL05DntdEsNTta2kyCBdfpQQ1J4Tj04sN4nm2JVe6wMz3Nq/KxD1+h7aMfa2sr8Pa4xeaYHrnut+CRi2kSLyCahJY8TC16/fSjmk=</DP><DQ>1wLFPCdkCVCekzKqneY2RxvqiKe+sHFTCo6CU4ifwvLl888TR59ymeeO6nsMHAoph7TLrlNDKwbCjIqyNDeLflATKXNDsh93EIwRpkUYPcOC8GGl56tmV9LASmG3qXOTMEaty3HPuF0wtZ6gmXsMNZHtwHntq3MPYt7fO7w9eCE=</DQ><InverseQ>6E3f+B2j++XK/k6rNOmQ87JNzuxLss63bAayXN9/qmUb+MpBV4iKwqd3RpGHsD9Dm619bh5OgDX63bmuvBctXKdM2f007FnWXwg5BrWv65zBT8NkBu+gJMeqvyvklbOm6UTR50QWRJGQdWzQx1bTbbBEudhwcRfdyOy0pWGhcxs=</InverseQ><D>GF7yT7C3azq/LapBUAulNJ7eaOk0p3K32Vz2nq5Dj41Wnq771EV2ufmyzaSpCa5gVYnegaFdHMpd3sf4vgBUlFuct5M97UFeAHgPiOSHZza7nscnJjbaoznEDVrsq6C7ytkrn+WyFhLsFNIie/UIKIYobkVNQwq8mzwNFSJgXiIaAMDiRPvcr8sFoOTIyru9JgNob+Z96DW0veM2WYzk7D/iG6xHEcaRz0iwrErNa4TryFdiIE/7GSXcN74UGmkjn6STNEtlS0U1PX1VCod2UkiUPQXeVB1aF99UTGjOKxvTLprmOnHV/mCa6kCXf1dJ13s5cZ6HZlsFdCrOpltwkQ==</D></RSAKeyValue>";

    internal const string PubKey =
        "<RSAKeyValue><Modulus>y4rFl2A+gXUJDN4zbNUChU3GxTtUKJBOK2qU8wMKui52S4tJzbxvjji5ze/4o1yN+gD67U1gJdOnK3uIAh936lgN78B/YUtN7ug3n04DcyJhVXehLJjWmlMJdwLnhHdiel8ELZYJdBODoRJHg8CVNRbmiOGco4LX1sjolQIraA9cFkcB2A2/ZGY+f8xj04kIVtqk90QpUrNoYIgk+sx6CXf40M1k9UrqkFQmH7Z/cH0Su34SeJuzW7wTAIpBTj9TK9eXSIXwi0EHBTVOk2AfwApaNF+5Le7UEKrkxHp8X5IiE9270jcvrtA2wCArEXBptjaXpofJYmoJYwNrzrN3Kw==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    internal static string ForRole(string role, string tenant = DefaultTenant, Guid? userId = null)
    {
        using var rsa = RSA.Create();
        rsa.FromXmlString(PrivKey);
        var key = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: true));
        var token = new JwtSecurityToken(
            claims:
            [
                new Claim("sub", (userId ?? Guid.NewGuid()).ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("tenant_id", tenant),
            ],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.RsaSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

sealed class NullEventBus : IEventBus
{
    public Task PublishAsync<TEvent>(TEvent @event, Guid tenantId, CancellationToken ct = default)
        where TEvent : class => Task.CompletedTask;
}

public sealed class ServiceFixture : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"test-rw-{Guid.NewGuid():N}";
    private readonly SqliteConnection _keepAlive;

    public ServiceFixture()
    {
        _keepAlive = new SqliteConnection($"Data Source={_dbName};Mode=Memory;Cache=Shared");
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var desc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ReimbursementDbContext>));
            if (desc is not null) services.Remove(desc);
            services.AddDbContext<ReimbursementDbContext>(o =>
                o.UseSqlite($"Data Source={_dbName};Mode=Memory;Cache=Shared"));

            // Replace the outbox-backed bus with a no-op; messaging is tested separately.
            services.RemoveAll<IEventBus>();
            services.AddScoped<IEventBus, NullEventBus>();

            // Stop outbox-dispatcher and event-subscriber background services
            services.RemoveAll<IHostedService>();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, opts =>
            {
                using var rsa = RSA.Create();
                rsa.FromXmlString(TokenFactory.PubKey);
                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new RsaSecurityKey(rsa.ExportParameters(includePrivateParameters: false)),
                };
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _keepAlive.Dispose();
        base.Dispose(disposing);
    }
}
