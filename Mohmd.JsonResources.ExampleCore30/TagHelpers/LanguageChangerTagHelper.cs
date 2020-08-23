using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Mohmd.JsonResources.ExampleCore30.TagHelpers
{
    [HtmlTargetElement("language-changer", TagStructure = TagStructure.NormalOrSelfClosing)]
    public class LanguageChangerTagHelper : TagHelper
    {
        private readonly HttpContext _httpContext;

        [HtmlAttributeName]
        public string Cultures { get; set; }

        public LanguageChangerTagHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContext = httpContextAccessor.HttpContext;
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = "div";
            output.Attributes.Add("class", "language-changer");
            
            output.Content.SetHtmlContent(@"
<form method=""get"" action=""/Home/ChangeLanguage"" id=""lang-form"">
    <input type=""hidden"" name=""returnUrl"" value=""" + _httpContext.Request.Path + @""" />
    <select name=""lang"" onchange=""langChanged()"">
        "+ string.Join("", GetCultureOptions()) + @"
    </select>
</form>
<script>
    function langChanged() {
        document.getElementById('lang-form').submit();
    }
</script>
");
        }

        private string[] GetCultureOptions()
        {
            string[] cultures = GetCultures();
            string active = CultureInfo.CurrentUICulture.Name;

            return cultures.Select(x =>
            {
                var info = CultureInfo.GetCultureInfo(x);
                if (x == active)
                {
                    return $"<option value='{ x }' selected>{ info.NativeName }</option>";
                }
                else
                {
                    return $"<option value='{ x }'>{ info.NativeName }</option>";
                }
            }).ToArray();
        }

        private string[] GetCultures()
        {
            if (string.IsNullOrEmpty(Cultures))
            {
                return new[] { "en-US", "fa-IR" };
            }

            return Cultures.Split(',', StringSplitOptions.RemoveEmptyEntries).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        }
    }
}
