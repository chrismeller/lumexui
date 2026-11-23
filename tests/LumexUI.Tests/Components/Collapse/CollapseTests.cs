// Copyright (c) LumexUI 2024
// LumexUI licenses this file to you under the MIT license
// See the license here https://github.com/chrismeller/lumexui/blob/main/LICENSE

using Microsoft.Extensions.DependencyInjection;

using TailwindMerge;

using static LumexUI.LumexCollapse;

namespace LumexUI.Tests.Components;

public class CollapseTests : BunitContext
{
    public CollapseTests()
    {
        Services.AddSingleton<TwMerge>();
		JSInterop.Setup<int>( "Lumex.elementReference.getScrollHeight", _ => true );
    }

    [Fact]
    public void Collapse_ShouldRenderCorrectly()
    {
        var action = () => Render<LumexCollapse>();

        action.Should().NotThrow();
    }

    [Fact]
    public void Collapse_InitiallyExpanded_ShouldBeExpanded()
    {
        var cut = Render<LumexCollapse>( p => p
            .Add( p => p.Expanded, true )
        );

        cut.Instance.State.Should().Be( CollapseState.Expanded );
    }

    [Fact]
    public void Collapse_OnExpandedChange_ShouldEnterCorrectState()
    {
        var cut = Render<LumexCollapse>();
        var collapse = cut.Find( "div" );

        cut.Render( p => p.Add( p => p.Expanded, true ) );
        cut.Instance.State.Should().Be( CollapseState.Expanding );

        collapse.TriggerEvent( "ontransitionend", new EventArgs() );
        cut.Instance.State.Should().Be( CollapseState.Expanded );

        cut.Render( p => p.Add( p => p.Expanded, false ) );
        cut.Instance.State.Should().Be( CollapseState.Collapsing );

        collapse.TriggerEvent( "ontransitionend", new EventArgs() );
        cut.Instance.State.Should().Be( CollapseState.Collapsed );
    }

    // TODO: Add other tests
}