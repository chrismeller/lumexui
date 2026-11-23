// Copyright (c) LumexUI 2024
// LumexUI licenses this file to you under the MIT license
// See the license here https://github.com/chrismeller/lumexui/blob/main/LICENSE

using LumexUI.Common;

using Microsoft.Extensions.DependencyInjection;

using TailwindMerge;

namespace LumexUI.Tests.Components;

public class DividerTests : BunitContext
{
    public DividerTests()
    {
        Services.AddSingleton<TwMerge>();
	}

    [Fact]
    public void Divider_ShouldRenderCorrectly()
    {
        var action = () => Render<LumexDivider>();

        action.Should().NotThrow();
    }

    [Fact]
    public void Divider_Horizontal_ShouldRenderAsHr()
    {
        var cut = Render<LumexDivider>( p => p
            .Add( p => p.Orientation, Orientation.Horizontal )
        );

        cut.Markup.Should().StartWith( "<hr" );
    }

    [Fact]
    public void Divider_Vertical_ShouldRenderAsDiv()
    {
        var cut = Render<LumexDivider>( p => p
            .Add( p => p.Orientation, Orientation.Vertical )
        );

        cut.Markup.Should().StartWith( "<div" );
    }
}