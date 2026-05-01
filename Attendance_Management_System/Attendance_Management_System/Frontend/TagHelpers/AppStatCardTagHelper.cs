using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Attendance_Management_System.Frontend.TagHelpers;

[HtmlTargetElement("app-stat-card")]
public class AppStatCardTagHelper : TagHelper
{
    [HtmlAttributeName("title")]
    public string Title { get; set; } = string.Empty;

    [HtmlAttributeName("value")]
    public string Value { get; set; } = string.Empty;

    [HtmlAttributeName("tone")]
    public string Tone { get; set; } = string.Empty;

    [HtmlAttributeName("subtitle")]
    public string? Subtitle { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "article";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "stat-card");

        var normalizedTone = (Tone ?? string.Empty).Trim().ToLowerInvariant();
        var valueClass = normalizedTone is "success" or "warning" or "danger"
            ? $"stat-value {normalizedTone}"
            : "stat-value";

        var builder = new StringBuilder();
        builder.Append("<p class=\"stat-title\">")
            .Append(System.Net.WebUtility.HtmlEncode(Title))
            .Append("</p>");

        builder.Append("<p class=\"")
            .Append(valueClass)
            .Append("\">")
            .Append(System.Net.WebUtility.HtmlEncode(Value))
            .Append("</p>");

        if (!string.IsNullOrWhiteSpace(Subtitle))
        {
            builder.Append("<p class=\"muted\">")
                .Append(System.Net.WebUtility.HtmlEncode(Subtitle))
                .Append("</p>");
        }

        output.Content.SetHtmlContent(builder.ToString());
    }
}

[HtmlTargetElement("app-info-card")]
public class AppInfoCardTagHelper : TagHelper
{
    [HtmlAttributeName("label")]
    public string Label { get; set; } = string.Empty;

    [HtmlAttributeName("value")]
    public string Value { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "article";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "info-card");

        var builder = new StringBuilder();
        builder.Append("<span class=\"muted\">")
            .Append(System.Net.WebUtility.HtmlEncode(Label))
            .Append("</span>");

        builder.Append("<strong>")
            .Append(System.Net.WebUtility.HtmlEncode(Value))
            .Append("</strong>");

        output.Content.SetHtmlContent(builder.ToString());
    }
}

[HtmlTargetElement("app-lazy-image")]
public class AppLazyImageTagHelper : TagHelper
{
    private readonly IUrlHelperFactory _urlHelperFactory;

    public AppLazyImageTagHelper(IUrlHelperFactory urlHelperFactory)
    {
        _urlHelperFactory = urlHelperFactory;
    }

    [HtmlAttributeName("src")]
    public string Src { get; set; } = string.Empty;

    [HtmlAttributeName("alt")]
    public string Alt { get; set; } = string.Empty;

    [HtmlAttributeName("class")]
    public string? Class { get; set; }

    [HtmlAttributeName("width")]
    public int? Width { get; set; }

    [HtmlAttributeName("height")]
    public int? Height { get; set; }

    [HtmlAttributeName("decoding")]
    public string Decoding { get; set; } = "async";

    [Microsoft.AspNetCore.Mvc.ViewFeatures.ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = default!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "img";
        output.TagMode = TagMode.SelfClosing;

        var urlHelper = _urlHelperFactory.GetUrlHelper(ViewContext);
        var resolvedSrc = urlHelper.Content(Src);

        output.Attributes.SetAttribute("src", "data:image/gif;base64,R0lGODlhAQABAAAAACw=");
        output.Attributes.SetAttribute("data-lazy-src", resolvedSrc);
        output.Attributes.SetAttribute("alt", Alt);
        output.Attributes.SetAttribute("loading", "lazy");
        output.Attributes.SetAttribute("decoding", Decoding);

        var classValue = string.IsNullOrWhiteSpace(Class)
            ? "lazy-media"
            : $"{Class} lazy-media";
        output.Attributes.SetAttribute("class", classValue);

        if (Width.HasValue && Width.Value > 0)
        {
            output.Attributes.SetAttribute("width", Width.Value);
        }

        if (Height.HasValue && Height.Value > 0)
        {
            output.Attributes.SetAttribute("height", Height.Value);
        }
    }
}
