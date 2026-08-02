using Bunit;
using DR.Simple_UI.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DR.Simple_UI.Tests;

/// <summary>
/// UserWidget's disclosure menu, its avatar fallbacks, and the scrim that dismisses it.
/// </summary>
public class UserWidgetTests : BunitContext
{
    [Fact]
    public void UserWidget_trigger_is_not_a_button_when_there_is_nothing_to_open()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Name, "Dennis Rahmen")
            .Add(c => c.Secondary, "rahmen@netpoint.de"));

        Assert.Equal("DIV", cut.Find(".user-trigger").NodeName);
        Assert.Equal("Dennis Rahmen", cut.Find(".user-name").TextContent);
        Assert.Equal("rahmen@netpoint.de", cut.Find(".user-email").TextContent);
        Assert.NotNull(cut.Find(".user-avatar i.ri-user-line"));
        Assert.Empty(cut.FindAll(".user-signout"));
    }

    [Fact]
    public void UserWidget_avatar_falls_back_from_image_to_initials_to_the_icon()
    {
        var image = Render<UserWidget>(p => p
            .Add(c => c.AvatarSrc, "/me.png").Add(c => c.Initials, "DR"));
        var initials = Render<UserWidget>(p => p.Add(c => c.Initials, "DR"));
        var icon = Render<UserWidget>(p => p.Add(c => c.Name, "X"));

        Assert.Equal("/me.png", image.Find(".user-avatar img").GetAttribute("src"));
        Assert.Equal("DR", initials.Find(".user-avatar span").TextContent);
        Assert.NotNull(icon.Find(".user-avatar i.ri-user-line"));
    }

    [Fact]
    public void UserWidget_menu_opens_on_click_and_closes_on_the_scrim()
    {
        var opened = new List<bool>();
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Name, "Dennis Rahmen")
            .Add(c => c.Menu, "<a class=\"menu-item\" href=\"/settings\">Settings</a>")
            .Add(c => c.OpenChanged, opened.Add));

        var trigger = cut.Find("button.user-trigger");
        Assert.Equal("false", trigger.GetAttribute("aria-expanded"));
        Assert.Empty(cut.FindAll(".menu"));

        trigger.Click();

        Assert.Equal("true", cut.Find("button.user-trigger").GetAttribute("aria-expanded"));
        Assert.NotNull(cut.Find(".menu .menu-item"));

        cut.Find(".menu-scrim").Click();

        Assert.Empty(cut.FindAll(".menu"));
        Assert.Equal([true, false], opened);
    }

    [Fact]
    public void UserWidget_menu_closes_on_escape()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Menu, "<button class=\"menu-item\">Settings</button>"));

        cut.Find("button.user-trigger").Click();
        Assert.NotNull(cut.Find(".menu"));

        cut.Find(".user-widget").KeyDown(Key.Escape);

        Assert.Empty(cut.FindAll(".menu"));
    }

    [Fact]
    public void UserWidget_scrim_is_ordered_before_the_panel()
    {
        // The panel paints above the scrim on source order plus its own z-index. If
        // the scrim came second it would cover the panel and eat the first click on
        // every menu item.
        var cut = Render<UserWidget>(p => p
            .Add(c => c.Menu, "<button class=\"menu-item\">S</button>"));
        cut.Find("button.user-trigger").Click();

        var children = cut.Find(".user-widget").Children.Select(c => c.GetAttribute("class")).ToArray();
        Assert.Equal(
            Array.IndexOf(children, "menu-scrim") + 1,
            Array.IndexOf(children, "menu"));
    }

    [Fact]
    public void UserWidget_sign_out_is_an_icon_only_link_with_a_name()
    {
        var cut = Render<UserWidget>(p => p
            .Add(c => c.SignOutHref, "/signout")
            .Add(c => c.SignOutTip, "Sign out of the console."));

        var link = cut.Find("a.user-signout");
        Assert.Equal("/signout", link.GetAttribute("href"));
        Assert.Equal("Sign out", link.GetAttribute("aria-label"));
        Assert.Equal("Sign out of the console.", link.GetAttribute("data-tip"));
    }
}
