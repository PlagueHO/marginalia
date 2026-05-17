using Marginalia.Tools.Cli;

var cancellationTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellationTokenSource.Cancel();
};

var rootCommand = CliRunner.BuildRootCommand((apiUrl, userId) =>
    new MarginaliaCliClient(CreateHttpClient(apiUrl, userId)));

Environment.ExitCode = await rootCommand.Parse(args).InvokeAsync();

static HttpClient CreateHttpClient(string apiUrl, string? userId)
{
    var httpClient = new HttpClient
    {
        BaseAddress = new Uri(apiUrl.TrimEnd('/') + "/", UriKind.Absolute),
        Timeout = TimeSpan.FromMinutes(5),
    };

    if (!string.IsNullOrWhiteSpace(userId))
    {
        httpClient.DefaultRequestHeaders.Add("X-User-Id", userId);
    }

    return httpClient;
}
