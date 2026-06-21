using Azure;
using Azure.Communication.Email;

namespace UnifiedRewards.ReportingCompliance.Handlers;

/// <summary>
/// Sends claim-event notifications via Azure Communication Services Email.
/// No-op unless ConnectionStrings:Acs + Notifications:FromAddress + Notifications:ToAddress are all set,
/// so existing behaviour is unchanged in local/dev. Failures are logged, never fatal.
///
/// NOTE: claim events carry the employee's Guid, not their email, so notifications go to a configured
/// Notifications:ToAddress (a demo/ops inbox). Emailing the actual employee would need an
/// employee-profile lookup — a documented follow-up.
/// </summary>
public sealed class EmailNotifier
{
    private readonly EmailClient? _client;
    private readonly string? _from;
    private readonly string? _to;
    private readonly ILogger<EmailNotifier> _logger;

    public EmailNotifier(IConfiguration config, ILogger<EmailNotifier> logger)
    {
        _logger = logger;
        var conn = config.GetConnectionString("Acs");
        _from = config["Notifications:FromAddress"];   // e.g. DoNotReply@<guid>.azurecomm.net
        _to   = config["Notifications:ToAddress"];     // demo/ops notification inbox
        if (!string.IsNullOrWhiteSpace(conn) && !string.IsNullOrWhiteSpace(_from) && !string.IsNullOrWhiteSpace(_to))
            _client = new EmailClient(conn);
    }

    public async Task NotifyAsync(string subject, string body, CancellationToken ct)
    {
        if (_client is null) return;
        try
        {
            await _client.SendAsync(WaitUntil.Completed, _from!, _to!, subject, htmlContent: $"<p>{body}</p>",
                cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ACS email notification failed (non-fatal).");
        }
    }
}
