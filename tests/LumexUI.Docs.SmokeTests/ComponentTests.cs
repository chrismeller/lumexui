namespace LumexUI.Docs.SmokeTests;

public sealed class ComponentTests( DocsSite site ) : DocsPageTest( site )
{
    [Fact]
    public async Task Popover_OpensNextToItsTrigger()
    {
        await GotoAsync( "/docs/components/popover" );

        var trigger = Page.GetByRole( AriaRole.Button, new() { Name = "Open Popover", Exact = true } ).First;
        var popover = Page.Locator( "[data-popover] [role=dialog]" );

        await UntilVisibleAsync( () => trigger.ClickAsync(), popover );

        await AssertNextToAsync( popover, trigger );
        Assert.Empty( Errors );
    }

    [Fact]
    public async Task Dropdown_OpensItsMenuNextToItsTrigger()
    {
        await GotoAsync( "/docs/components/dropdown" );

        var trigger = Page.GetByRole( AriaRole.Button, new() { Name = "Open Dropdown", Exact = true } ).First;
        var menu = Page.Locator( "[data-popover] [role=dialog]" );

        await UntilVisibleAsync( () => trigger.ClickAsync(), menu );

        await Expect( menu.GetByText( "New file" ) ).ToBeVisibleAsync();
        await AssertNextToAsync( menu, trigger );
        Assert.Empty( Errors );
    }

    [Fact]
    public async Task Tooltip_ShowsNextToItsTriggerOnHover()
    {
        await GotoAsync( "/docs/components/tooltip" );

        var trigger = Page.GetByRole( AriaRole.Button, new() { Name = "Hover me", Exact = true } ).First;
        var tooltip = Page.Locator( "[data-popover] [role=tooltip]" ).Filter( new() { HasText = "I am a tooltip" } );

        await UntilVisibleAsync( async () =>
        {
            // Hover is only seen as a change of position, so move away before hovering again.
            await Page.Mouse.MoveAsync( 0, 0 );
            await trigger.HoverAsync();
        }, tooltip );

        await AssertNextToAsync( tooltip, trigger );
        Assert.Empty( Errors );
    }

    [Fact]
    public async Task Modal_OpensTrapsFocusAndCloses()
    {
        await GotoAsync( "/docs/components/modal" );

        var trigger = Page.GetByRole( AriaRole.Button, new() { Name = "Open Modal", Exact = true } ).First;
        var modal = Page.Locator( "dialog[open]" );

        await UntilVisibleAsync( () => trigger.ClickAsync(), modal );
        await Expect( modal ).ToContainTextAsync( "Welcome to LumexUI" );

        // Tab more times than the modal has focusable elements, so focus would have to leave it if it could. A modal
        // <dialog> hands focus to the browser's own UI after its last element (the page then reports <body> as
        // focused) and back to its first; it must never reach an element of the page behind it.
        for( var i = 0; i < 6; i++ )
        {
            await Page.Keyboard.PressAsync( "Tab" );
            var focused = await modal.EvaluateAsync<string?>(
                """
                dialog => {
                    const active = document.activeElement;
                    if (!active || active === document.body || active === dialog || dialog.contains(active)) {
                        return null;
                    }
                    return active.outerHTML.slice(0, 200);
                }
                """ );
            Assert.True( focused is null, $"Focus left the open modal after {i + 1} Tab presses, to {focused}" );
        }

        await Page.Keyboard.PressAsync( "Escape" );
        await Expect( modal ).ToHaveCountAsync( 0 );
        Assert.Empty( Errors );
    }

    [Fact]
    public async Task Textarea_GrowsWithItsContent()
    {
        await GotoAsync( "/docs/components/textarea" );

        // The examples' labels aren't associated with their textareas, so find them by placeholder.
        var autosize = Page.GetByPlaceholder( "Type until the textarea grows...", new() { Exact = true } );
        var fixedSize = Page.GetByPlaceholder( "Locked at 4 rows; never grows.", new() { Exact = true } );

        var before = await HeightAsync( autosize );
        var fixedBefore = await HeightAsync( fixedSize );

        // Typing starts the autosize script once the component is interactive; retry until it has.
        var lines = string.Join( "\n", Enumerable.Range( 1, 6 ).Select( n => $"Line {n}" ) );
        var deadline = DateTime.UtcNow.AddSeconds( 30 );
        double after;
        do
        {
            await autosize.FillAsync( "" );
            await autosize.PressSequentiallyAsync( lines );
            after = await HeightAsync( autosize );
        }
        while( after <= before && DateTime.UtcNow < deadline );

        Assert.True( after > before, $"The autosize textarea stayed {before}px high with six lines of text." );

        await fixedSize.PressSequentiallyAsync( lines );
        Assert.Equal( fixedBefore, await HeightAsync( fixedSize ) );
        Assert.Empty( Errors );
    }

    [Fact]
    public async Task ThemeToggle_SwitchesBetweenLightAndDark()
    {
        await GotoAsync( "/docs/getting-started/overview" );

        var html = Page.Locator( "html" );
        var toggle = Page.GetByRole( AriaRole.Button, new() { Name = "Toggle theme", Exact = true } )
            .Locator( "visible=true" );

        // Headless Chromium prefers light, so the system theme resolves to light.
        await Expect( html ).ToHaveClassAsync( new System.Text.RegularExpressions.Regex( @"\blight\b" ) );

        await UntilVisibleAsync( () => toggle.ClickAsync(), Page.Locator( "html.dark" ) );
        Assert.Equal( "dark", await Page.EvaluateAsync<string>( "localStorage.getItem('lumexui.theme')" ) );

        await toggle.ClickAsync();
        await Expect( html ).ToHaveClassAsync( new System.Text.RegularExpressions.Regex( @"\blight\b" ) );
        Assert.Equal( "light", await Page.EvaluateAsync<string>( "localStorage.getItem('lumexui.theme')" ) );
        Assert.Empty( Errors );
    }

    private static async Task<double> HeightAsync( ILocator locator )
    {
        var box = await locator.BoundingBoxAsync() ?? throw new InvalidOperationException( "The element has no box." );
        return box.Height;
    }
}
