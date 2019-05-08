using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Board
{
    [HtmlTargetElement(Attributes = "issel")]
    public class MyOptionsTagHelper : TagHelper
    {
        [HtmlAttributeName("issel")]
        public bool IsSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            base.Process(context, output);
            if (IsSelected) output.Attributes.Add("selected", "");
        }
    }
}
