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
            Errors.Clear();
            // Wait for Blazor and for the WebAssembly download and start, including the example iframes on the navbar page,
            // each of which starts its own Blazor.
            await GotoAsync( path, WaitUntilState.Load );
            await Page.WaitForFunctionAsync( "() => window.Blazor !== undefined" );
            await WaitForNetworkQuietAsync( TimeSpan.FromSeconds( 1 ), TimeSpan.FromSeconds( 90 ) );

            failures.AddRange( Errors.Select( error => $"{path}: {error}" ) );
        }

        Assert.True( failures.Count == 0, string.Join( "\n", failures ) );
    }
}
