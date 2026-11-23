using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace LumexUI.Docs.SmokeTests;

/// <summary>
/// Gives each test its own browser context and page on the docs site, and records the page's console errors.
/// </summary>
[Collection( DocsSiteCollection.Name )]
public abstract class DocsPageTest( DocsSite site ) : IAsyncLifetime
{
    private static readonly TimeSpan InteractiveTimeout = TimeSpan.FromSeconds( 30 );
    private static readonly Regex FetchCannotLoad = new( @"Fetch API cannot load (?<url>\S+?)\.?\s*$" );

    private readonly List<string> _errors = [];
    private readonly ConcurrentDictionary<string, byte> _abortedInFrames = new();

    private IBrowserContext _context = null!;
    private long _requests;
    private long _settled;

    protected DocsSite Site { get; } = site;

    protected IPage Page { get; private set; } = null!;

    /// <summary>
    /// Console errors and uncaught exceptions on the docs site's own origin. Other hosts don't resolve (see
    /// <see cref="DocsSite"/>), and the errors for those failed requests are reported at their own URL, so they
    /// are not listed.
    /// <para>
    /// Also left out: "Fetch API cannot load" for a request that an example iframe aborted. On the navbar page
    /// Blazor replaces the prerendered example iframes, and the replaced frames abort the WebAssembly downloads
    /// they had started. A download that fails for any other reason is still listed.
    /// </para>
    /// </summary>
    protected IReadOnlyList<string> Errors
    {
        get
        {
            lock( _errors )
            {
                return _errors.Where( error => !IsAbortedInFrame( error ) ).ToList();
            }
        }
    }

    protected void ClearErrors()
    {
        lock( _errors )
        {
            _errors.Clear();
        }

        _abortedInFrames.Clear();
    }

    public async ValueTask InitializeAsync()
    {
        _context = await Site.Browser.NewContextAsync( new() { BaseURL = Site.BaseUrl } );

        _context.Request += ( _, _ ) => Interlocked.Increment( ref _requests );
        _context.RequestFinished += ( _, _ ) => Interlocked.Increment( ref _settled );
        _context.RequestFailed += ( _, request ) =>
        {
            if( request.Failure == "net::ERR_ABORTED" && request.Frame != Page.MainFrame )
            {
                _abortedInFrames.TryAdd( request.Url, 0 );
            }

            Interlocked.Increment( ref _settled );
        };

        Page = await _context.NewPageAsync();
        Page.Console += ( _, message ) =>
        {
            if( message.Type == "error" && message.Location.StartsWith( Site.BaseUrl, StringComparison.Ordinal ) )
            {
                AddError( $"{message.Location}: {message.Text}" );
            }
        };
        Page.PageError += ( _, error ) => AddError( $"Uncaught: {error}" );
    }

    private void AddError( string error )
    {
        lock( _errors )
        {
            _errors.Add( error );
        }
    }

    private bool IsAbortedInFrame( string error )
    {
        var match = FetchCannotLoad.Match( error );
        return match.Success && _abortedInFrames.ContainsKey( match.Groups["url"].Value );
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    protected async Task GotoAsync( string path, WaitUntilState waitUntil = WaitUntilState.NetworkIdle )
    {
        var response = await Page.GotoAsync( path, new() { WaitUntil = waitUntil } );
        Assert.NotNull( response );
        Assert.Equal( 200, response.Status );
    }

    /// <summary>
    /// Waits until no request has started or been outstanding for <paramref name="quiet"/>. Playwright's NetworkIdle
    /// never arrives on the navbar page: Blazor replaces the prerendered example iframes, and the frames it removes
    /// mid-load stay pending for Playwright, although every request has finished.
    /// </summary>
    protected async Task WaitForNetworkQuietAsync( TimeSpan quiet, TimeSpan timeout )
    {
        var deadline = DateTime.UtcNow + timeout;
        var lastChange = DateTime.UtcNow;
        var lastSeen = Interlocked.Read( ref _requests );

        while( true )
        {
            await Task.Delay( 100 );

            var started = Interlocked.Read( ref _requests );
            var settled = Interlocked.Read( ref _settled );
            if( started != lastSeen || settled < started )
            {
                lastSeen = started;
                lastChange = DateTime.UtcNow;
            }
            else if( DateTime.UtcNow - lastChange >= quiet )
            {
                return;
            }

            if( DateTime.UtcNow > deadline )
            {
                throw new TimeoutException(
                    $"The page kept loading for {timeout.TotalSeconds}s ({started} requests started, {settled} settled)." );
            }
        }
    }

    /// <summary>
    /// Pages are prerendered, so a control is on screen before Blazor WebAssembly has started and attached its
    /// handlers. Repeats <paramref name="act"/> until <paramref name="expected"/> is visible, so the test waits for
    /// interactivity instead of for a fixed time.
    /// </summary>
    protected async Task UntilVisibleAsync( Func<Task> act, ILocator expected )
    {
        var deadline = DateTime.UtcNow + InteractiveTimeout;
        while( true )
        {
            await act();
            try
            {
                await expected.WaitForAsync( new() { State = WaitForSelectorState.Visible, Timeout = 1000 } );
                return;
            }
            catch( TimeoutException ) when( DateTime.UtcNow < deadline )
            {
                // Not interactive yet; try again.
            }
            catch( TimeoutException e )
            {
                throw new TimeoutException(
                    $"{expected} didn't become visible within {InteractiveTimeout.TotalSeconds}s of retrying.", e );
            }
        }
    }

    /// <summary>
    /// Asserts that <paramref name="floating"/> sits next to <paramref name="anchor"/>: within
    /// <paramref name="maxGap"/> pixels of it on one axis and overlapping it on the other. A broken positioning
    /// script leaves the element at the page origin or far from its trigger.
    /// </summary>
    protected static async Task AssertNextToAsync( ILocator floating, ILocator anchor, double maxGap = 24 )
    {
        var f = await floating.BoundingBoxAsync() ?? throw new InvalidOperationException( "The floating element has no box." );
        var a = await anchor.BoundingBoxAsync() ?? throw new InvalidOperationException( "The anchor has no box." );

        var verticalGap = Math.Max( f.Y - ( a.Y + a.Height ), a.Y - ( f.Y + f.Height ) );
        var horizontalGap = Math.Max( f.X - ( a.X + a.Width ), a.X - ( f.X + f.Width ) );
        var overlapsHorizontally = horizontalGap < 0;
        var overlapsVertically = verticalGap < 0;

        var description = $"floating {{x={f.X}, y={f.Y}, w={f.Width}, h={f.Height}}}, " +
                          $"anchor {{x={a.X}, y={a.Y}, w={a.Width}, h={a.Height}}}";

        Assert.True(
            ( overlapsHorizontally && verticalGap >= -1 && verticalGap <= maxGap ) ||
            ( overlapsVertically && horizontalGap >= -1 && horizontalGap <= maxGap ),
            $"Expected the floating element within {maxGap}px of its anchor: {description}" );
    }
}
