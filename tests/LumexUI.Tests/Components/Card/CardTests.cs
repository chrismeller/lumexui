// Copyright (c) LumexUI 2024
// LumexUI licenses this file to you under the MIT license
// See the license here https://github.com/chrismeller/lumexui/blob/main/LICENSE

using LumexUI.Common;

using Microsoft.Extensions.DependencyInjection;

using TailwindMerge;

namespace LumexUI.Tests.Components;

public class CardTests : BunitContext
{
    public CardTests()
    {
        Services.AddSingleton<TwMerge>();
	}

    [Fact]
    public void Card_ShouldRenderCorrectly()
    {
        var action = () => Render<LumexCard>();

        action.Should().NotThrow();
    }

    [Fact]
    public void CardHeader_ShouldRenderCorrectly()
    {
        var action = () => Render<LumexCard>( p => p
            .AddChildContent<LumexCardHeader>()
            .AddChildContent<LumexCardBody>()
            .AddChildContent<LumexCardFooter>()
        );

        action.Should().NotThrow();
    }

    [Fact]
    public void CardHeader_StandaloneUsage_ShouldThrowInvalidOperation()
    {
        var action = () => Render<LumexCardHeader>();

        action.Should().Throw<ContextNullException>();
    }

    [Fact]
    public void CardBody_StandaloneUsage_ShouldThrowInvalidOperation()
    {
        var action = () => Render<LumexCardBody>();

        action.Should().Throw<ContextNullException>();
    }

    [Fact]
    public void CardFooter_StandaloneUsage_ShouldThrowInvalidOperation()
    {
        var action = () => Render<LumexCardFooter>();

        action.Should().Throw<ContextNullException>();
    }
}