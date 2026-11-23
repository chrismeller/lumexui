using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LumexUI.Docs.SmokeTests;

/// <summary>
/// Starts the published docs site and one Chromium for every test in the collection.
/// </summary>
public sealed class DocsSite : IAsyncLifetime
{
    public const string PathVariable = "LUMEXUI_DOCS_PATH";

    private readonly StringBuilder _serverOutput = new();
    private Process? _server;
    private IPlaywright? _playwright;

    public string BaseUrl { get; private set; } = "";

    public IBrowser Browser { get; private set; } = null!;

    public string ServerOutput
    {
        get { lock( _serverOutput ) { return _serverOutput.ToString(); } }
    }

    public async ValueTask InitializeAsync()
    {
        var publishDir = Environment.GetEnvironmentVariable( PathVariable );
        if( string.IsNullOrWhiteSpace( publishDir ) )
        {
            throw new InvalidOperationException(
                $"Set {PathVariable} to the output of `dotnet publish docs/LumexUI.Docs -c Release -o <dir>`." );
        }

        var dll = Path.Combine( Path.GetFullPath( publishDir ), "LumexUI.Docs.dll" );
        if( !File.Exists( dll ) )
        {
            throw new InvalidOperationException( $"{dll} does not exist. Is {PathVariable} a docs publish directory?" );
        }

        BaseUrl = $"http://127.0.0.1:{GetFreePort()}";
        StartServer( dll );
        await WaitForServerAsync();

        // Downloads the Chromium build this Playwright version expects, if it isn't there yet.
        var exitCode = Microsoft.Playwright.Program.Main( ["install", "chromium"] );
        if( exitCode != 0 )
        {
            throw new InvalidOperationException( $"`playwright install chromium` exited with code {exitCode}." );
        }

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync( new()
        {
            // The docs pages load a chart library from a CDN, and the data grid examples call a public API. Neither is
            // LumexUI, and neither should make these tests depend on the internet, so no other host resolves.
            // (Routing the requests away instead would turn off the browser cache, and every example iframe would
            // download the WebAssembly runtime again.)
            Args = ["--host-resolver-rules=MAP * ~NOTFOUND, EXCLUDE 127.0.0.1"],
        } );
    }

    public async ValueTask DisposeAsync()
    {
        if( Browser is not null )
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();

        if( _server is { HasExited: false } )
        {
            _server.Kill( entireProcessTree: true );
            await _server.WaitForExitAsync();
        }

        _server?.Dispose();
    }

    private void StartServer( string dll )
    {
        var contentRoot = Path.GetDirectoryName( dll )!;
        var startInfo = new ProcessStartInfo( "dotnet" )
        {
            WorkingDirectory = contentRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        startInfo.ArgumentList.Add( dll );
        startInfo.ArgumentList.Add( "--contentRoot" );
        startInfo.ArgumentList.Add( contentRoot );
        startInfo.Environment["ASPNETCORE_URLS"] = BaseUrl;
        startInfo.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";

        _server = new Process { StartInfo = startInfo };
        _server.OutputDataReceived += ( _, e ) => AppendOutput( e.Data );
        _server.ErrorDataReceived += ( _, e ) => AppendOutput( e.Data );
        _server.Start();
        _server.BeginOutputReadLine();
        _server.BeginErrorReadLine();
    }

    private async Task WaitForServerAsync()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds( 5 ) };
        var deadline = DateTime.UtcNow.AddSeconds( 60 );

        while( DateTime.UtcNow < deadline )
        {
            if( _server!.HasExited )
            {
                throw new InvalidOperationException(
                    $"The docs site exited with code {_server.ExitCode}:\n{ServerOutput}" );
            }

            try
            {
                using var response = await http.GetAsync( BaseUrl );
                if( response.StatusCode == HttpStatusCode.OK )
                {
                    return;
                }
            }
            catch( HttpRequestException )
            {
                // Not listening yet.
            }
            catch( TaskCanceledException )
            {
                // Not answering yet.
            }

            await Task.Delay( 250 );
        }

        throw new TimeoutException( $"The docs site didn't answer within 60 seconds:\n{ServerOutput}" );
    }

    private void AppendOutput( string? line )
    {
        if( line is null )
        {
            return;
        }

        lock( _serverOutput )
        {
            _serverOutput.AppendLine( line );
        }
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener( IPAddress.Loopback, 0 );
        listener.Start();
        return ( (IPEndPoint)listener.LocalEndpoint ).Port;
    }
}

[CollectionDefinition( Name )]
public sealed class DocsSiteCollection : ICollectionFixture<DocsSite>
{
    public const string Name = "Docs site";
}
