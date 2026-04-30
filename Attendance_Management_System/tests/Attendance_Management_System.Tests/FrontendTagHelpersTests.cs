using Attendance_Management_System.Frontend.TagHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Moq;

namespace Attendance_Management_System.Tests;

public class FrontendTagHelpersTests
{
    [Fact]
    public void AppStatCardTagHelper_RendersExpectedMarkupWithToneAndSubtitle()
    {
        var tagHelper = new AppStatCardTagHelper
        {
            Title = "Present",
            Value = "21",
            Tone = "success",
            Subtitle = "91%"
        };

        var output = Execute(tagHelper, "app-stat-card");

        Assert.Equal("article", output.TagName);
        Assert.Equal("stat-card", output.Attributes["class"]?.Value?.ToString());

        var html = output.Content.GetContent();
        Assert.Contains("<p class=\"stat-title\">Present</p>", html);
        Assert.Contains("<p class=\"stat-value success\">21</p>", html);
        Assert.Contains("<p class=\"muted\">91%</p>", html);
    }

    [Fact]
    public void AppInfoCardTagHelper_RendersExpectedMarkup()
    {
        var tagHelper = new AppInfoCardTagHelper
        {
            Label = "Course",
            Value = "BSIT"
        };

        var output = Execute(tagHelper, "app-info-card");

        Assert.Equal("article", output.TagName);
        Assert.Equal("info-card", output.Attributes["class"]?.Value?.ToString());

        var html = output.Content.GetContent();
        Assert.Contains("<span class=\"muted\">Course</span>", html);
        Assert.Contains("<strong>BSIT</strong>", html);
    }

    [Fact]
    public void AppLazyImageTagHelper_RendersLazyAttributes()
    {
        var urlHelper = new Mock<IUrlHelper>();
        urlHelper
            .Setup(helper => helper.Content("~/images/crest.png"))
            .Returns("/images/crest.png");

        var factory = new Mock<IUrlHelperFactory>();
        factory
            .Setup(created => created.GetUrlHelper(It.IsAny<ActionContext>()))
            .Returns(urlHelper.Object);

        var tagHelper = new AppLazyImageTagHelper(factory.Object)
        {
            Src = "~/images/crest.png",
            Alt = "Crest",
            Class = "hero-logo",
            Width = 320,
            Height = 220,
            ViewContext = new ViewContext()
        };

        var output = Execute(tagHelper, "app-lazy-image");

        Assert.Equal("img", output.TagName);
        Assert.Equal("hero-logo lazy-media", output.Attributes["class"]?.Value?.ToString());
        Assert.Equal("/images/crest.png", output.Attributes["data-lazy-src"]?.Value?.ToString());
        Assert.Equal("lazy", output.Attributes["loading"]?.Value?.ToString());
        Assert.Equal("async", output.Attributes["decoding"]?.Value?.ToString());
        Assert.Equal("Crest", output.Attributes["alt"]?.Value?.ToString());
        Assert.Equal("320", output.Attributes["width"]?.Value?.ToString());
        Assert.Equal("220", output.Attributes["height"]?.Value?.ToString());
    }

    private static TagHelperOutput Execute(TagHelper tagHelper, string tagName)
    {
        var context = new TagHelperContext(
            tagName,
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

        var output = new TagHelperOutput(
            tagName,
            new TagHelperAttributeList(),
            (_, _) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        tagHelper.Process(context, output);
        return output;
    }
}
