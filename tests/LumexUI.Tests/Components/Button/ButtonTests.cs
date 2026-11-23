// Copyright (c) LumexUI 2024
// LumexUI licenses this file to you under the MIT license
// See the license here https://github.com/chrismeller/lumexui/blob/main/LICENSE

using LumexUI.Common;
using LumexUI.Extensions;

using Microsoft.Extensions.DependencyInjection;

using TailwindMerge;

namespace LumexUI.Tests.Components;

public class ButtonTests : BunitContext
{
	public ButtonTests()
	{
		Services.AddSingleton<TwMerge>();
	}

	[Fact]
	public void Button_ShouldRenderCorrectly()
	{
		var action = () => Render<LumexButton>();

		action.Should().NotThrow();
	}

	[Fact]
	public void Button_Disabled_ShouldHaveDisabledAttribute()
	{
		var cut = Render<LumexButton>( p => p
			.Add( p => p.Disabled, true )
		);

		var button = cut.Find( "button" );

		button.HasAttribute( "disabled" ).Should().BeTrue();
	}

	[Fact]
	public void Button_Disabled_ShouldNotTriggerClick()
	{
		var clicked = false;
		var cut = Render<LumexButton>( p => p
			.Add( p => p.Disabled, true )
			.Add( p => p.OnClick, () => clicked = true )
		);

		var button = cut.Find( "button" );
		button.Click();

		clicked.Should().BeFalse();
	}

	[Fact]
	public void Button_NotDisabled_ShouldTriggerClick()
	{
		var clicked = false;
		var cut = Render<LumexButton>( p => p
			.Add( p => p.Disabled, false )
			.Add( p => p.OnClick, () => clicked = true )
		);

		var button = cut.Find( "button" );
		button.Click();

		clicked.Should().BeTrue();
	}

	[Fact]
	public void Button_Type_ShouldHaveCorrectTypeAttribute()
	{
		var cut = Render<LumexButton>( p => p
			.Add( p => p.Type, ButtonType.Submit )
		);

		var button = cut.Find( "button" );

		button.GetAttribute( "type" ).Should().Be( ButtonType.Submit.ToDescription() );
	}

	[Fact]
	public void Button_StartContent_ShouldRenderWithStartContent()
	{
		var cut = Render<LumexButton>( p => p
			.Add( p => p.StartContent, "start-content" )
		);

		cut.Markup.Should().Contain( "start-content" );
	}

	[Fact]
	public void Button_EndContent_ShouldRenderWithEndContent()
	{
		var cut = Render<LumexButton>( p => p
			.Add( p => p.EndContent, "end-content" )
		);

		cut.Markup.Should().Contain( "end-content" );
	}
}