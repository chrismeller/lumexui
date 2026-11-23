namespace LumexUI.Docs.SmokeTests;

public sealed class ConsoleErrorTests( DocsSite site ) : DocsPageTest( site )
{
    [Fact]
    public async Task ComponentPages_LogNoErrors()
    {
        // Take the page list from the site's own navigation, so a new component page is covered without a change here.
        await GotoAsync( "/docs/getting-started/overview" );
        var paths = ( await Page.Locator( "a[href*='docs/components/']" ).EvaluateAllAsync<string[]>(
                "links => links.map(link => new URL(link.href).pathname)" ) )
            .Distinct()
            .Order( StringComparer.Ordinal )
            .ToList();

        Assert.True( paths.Count >= 30, $"Expected the navigation to list at least 30 component pages, found {paths.Count}." );

        var failures = new List<string>();
        foreach( var path in paths )
        {
            ClearErrors();
            var serverOutputStart = Site.ServerOutput.Length;
            // Wait for Blazor and for the WebAssembly download and start, including the example iframes on the navbar page,
            // each of which starts its own Blazor.
            await GotoAsync( path, WaitUntilState.Load );
            await Page.WaitForFunctionAsync( "() => window.Blazor !== undefined" );
            await WaitForNetworkQuietAsync( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 90 ) );

            failures.AddRange( Errors.Select( error => $"{path}: {error}" ) );

            // A page that fails while the server prerenders it still answers 200, because the headers are already
            // sent, and the browser only gets a cut-off page. The server's log is where that shows.
            failures.AddRange( Site.ServerOutput[serverOutputStart..]
                .Split( '\n' )
                .Where( line => line.StartsWith( "fail:", StringComparison.Ordinal ) || line.StartsWith( "crit:", StringComparison.Ordinal ) )
                .Select( line => $"{path}: server logged {line.TrimEnd()}" ) );
        }

        Assert.True( failures.Count == 0, string.Join( "\n", failures ) );
    }
}
