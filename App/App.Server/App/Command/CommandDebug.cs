using System.Net;
using System.Net.Mail;

public class CommandDebug(CommandContextService context, DataService dataService, AiService ai, ConfigurationService configuration, StorageService storage)
{
    public async Task<DebugDto> Run()
    {
        var result = new DebugDto
        {
            VersionServer = UtilServer.VersionServer,
            Instance = dataService.Instance,
            Counter = dataService.Counter,
            CounterList = dataService.CounterList,
            McpUrl = configuration.McpUrl(),
            Text = await ai.AnalyzeDocumentAsync("Doc1.pdf", storage)
        };

        result.Text += "; Workflow=" + await ai.WorkflowRun();

        context.NotificationAdd("Hello from debug", NotificationEnum.Info);

        if (context.RequestSessionId == null)
        {
            context.ResponseSessionId = Guid.NewGuid().ToString();
        }
        else
        {
            context.NotificationAdd("SessionId=" + context.RequestSessionId, NotificationEnum.Info);
        }

        var yourCode = "Your Code: " + Random.Shared.Next(100000, 1000000);

        // Mail
        using var client = new SmtpClient(configuration.EmailHost, configuration.EmailPort ?? -1)
        {
            Credentials = new NetworkCredential(configuration.EmailFrom, configuration.EmailPassword),
            EnableSsl = true
        };
        var mail = new MailMessage(configuration.EmailFrom!, configuration.EmailTo!, "Hello World", "This is the body." + " " + yourCode);
        mail.Bcc.Add(configuration.EmailFrom!); // Keep copy
        client.Send(mail);

        // SMS (Twilio)
        using var httpClient = new HttpClient();
        var credentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{configuration.SmsAccount}:{configuration.SmsToken}"));
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
        await httpClient.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{configuration.SmsAccount}/Messages.json",
            new FormUrlEncodedContent([
                new KeyValuePair<string, string>("To", configuration.SmsTo!),
                new KeyValuePair<string, string>("From", configuration.SmsFrom!),
                new KeyValuePair<string, string>("Body", "Hello world" + " " + yourCode)
            ]));

        return result;
    }
}

public class DebugDto
{
    public string? VersionServer { get; set; }
    
    public int? Instance { get; set; }

    public int? Counter { get; set; }

    public List<string>? CounterList { get; set; }

    public string? AiChat { get; set; }
    
    public string? McpUrl { get; set; }

    public string? Text { get; set; }
}
