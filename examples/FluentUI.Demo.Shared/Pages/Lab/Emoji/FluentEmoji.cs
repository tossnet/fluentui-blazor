﻿using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Fast.Components.FluentUI;
using Microsoft.Fast.Components.FluentUI.Infrastructure;

namespace FluentUI.Demo.Shared;

public partial class FluentEmoji : FluentComponentBase
{
    //private const string EMOJI_ROOT = "_content/Microsoft.Fast.Components.FluentUI/emojis";
    private const string EMOJI_ROOT = "_content/FluentUI.Demo.Shared/emojis";
    private string? _svg;
    private string? _emojiUrl;
    private string? _folder;
    private string? _group;
    private string? _prevResult;

    private bool hasSkintone = false;
    //private int _size;


    [Inject]
    private StaticAssetService StaticAssetService { get; set; } = default!;

    [Inject]
    private CacheStorageAccessor CacheStorageAccessor { get; set; } = default!;

    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the emoji. Can be specified by using const value from <see cref="FluentEmojis" />
    /// </summary>
    [Parameter]
    [EditorRequired]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the <see cref="EmojiStyle"/> to use. 
    /// Defaults to Color
    /// </summary>
    [Parameter]
    public EmojiStyle? EmojiStyle { get; set; } = Demo.Shared.EmojiStyle.Color;

    /// <summary>
    /// Gets or sets the <see cref="EmojiSkintone"/> to use.
    /// Defaults to Default
    /// </summary>
    [Parameter]
    public EmojiSkintone? Skintone { get; set; } = Demo.Shared.EmojiSkintone.Default;

    /// <summary>
    /// Gets or sets the <see cref="EmojiSize"/> of the emoji. Defaults to Size32.
    /// </summary>
    [Parameter]
    public EmojiSize Size { get; set; } = EmojiSize.Size32;

    /// <summary>
    /// Gets or sets the custom size of the emoji. Can only be set if Size is set to Custom.
    /// </summary>
    [Parameter]
    public int? CustomSize { get; set; }

    /// <summary>
    /// Gets or sets the slot where the emoji is displayed in
    /// </summary>
    [Parameter]
    public string? Slot { get; set; } = null;


    /// <summary>
    /// Allows for capturing a mouse click on an emoji
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }


    //protected override async Task OnParametersSetAsync()
    protected override void OnParametersSet()
    {
        if (Size == EmojiSize.Custom && CustomSize == null)
        {
            throw new ArgumentException("CustomSize must be set if Size is set to Custom");
        }
        if (Size != EmojiSize.Custom && CustomSize != null)
        {
            throw new ArgumentException("CustomSize can only be set if Size is set to Custom");
        }
        if (CustomSize > 1024)
        {
            throw new ArgumentException("CustomSize can not be larger than 1024");
        }

        EmojiModel model = FluentEmojis.EmojiMap.First(x => x.Name == Name);
        _folder = model.Folder;
        _group = model.Group.ToString();

        if (model.Skintone.HasValue)
            hasSkintone = true;

        _emojiUrl = $"{EMOJI_ROOT}/{_group}/{_folder}/{GetComposedName()}.svg";

    }


    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        string result;

        if (!string.IsNullOrEmpty(_emojiUrl) )
        {
            HttpRequestMessage? message = CreateMessage(_emojiUrl);
            // Get the result from the cache
            result = await CacheStorageAccessor.GetAsync(message);

            if (string.IsNullOrEmpty(result))
            {
                //It is not in the cache, get it from the StaticAssetService (download)
                HttpResponseMessage? response = await StaticAssetService.HttpClient.SendAsync(message);


                if (response.IsSuccessStatusCode)
                {
                    // Store the response in the cache and get the result
                    result = await CacheStorageAccessor.PutAndGetAsync(message, response);
                }
                else
                    result = string.Empty;
            }

            if (!string.IsNullOrEmpty(result))
            {
                Regex regex = new("<svg (?<attributes>.*?)>(?<payload>.*?)</svg>", RegexOptions.Singleline);

                MatchCollection matches = regex.Matches(result);
                Match? match = matches.FirstOrDefault();
                if (match is not null)
                {
                    result = match.Groups["payload"].Value;
                }

                if (_prevResult != result)
                {
                    _svg = result;
                    _prevResult = result;

                    StateHasChanged();
                }
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!string.IsNullOrEmpty(_svg))
        {
            int size = Size == EmojiSize.Custom ? CustomSize!.Value : (int)Size;

            builder.OpenElement(0, "svg");
            builder.AddAttribute(1, "id", Id);
            builder.AddAttribute(2, "slot", Slot);
            builder.AddAttribute(3, "class", Class);
            builder.AddAttribute(4, "width", size);
            builder.AddAttribute(5, "height", size);
            builder.AddAttribute(6, "viewBox", "0 0 32 32");
            builder.AddAttribute(7, "fill", "none");
            builder.AddAttribute(8, "xmlns", "http://www.w3.org/2000/svg");
            builder.AddAttribute(9, "onclick", OnClickHandler);
            builder.AddMultipleAttributes(10, AdditionalAttributes);
            builder.AddMarkupContent(11, _svg);
            builder.CloseElement();
        }
    }

    protected async Task OnClickHandler(MouseEventArgs e)
    {
        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync(e);
    }

    private string GetComposedName()
    {

        string result = "";
        if (Name is not null)
        {
            if (hasSkintone)
                result = $"{Name}_{EmojiStyle.ToAttributeValue()}_{Skintone.ToAttributeValue()}";
            else
                result = $"{Name}_{EmojiStyle.ToAttributeValue()}";
        }

        return result;
    }

    private static HttpRequestMessage CreateMessage(string url) => new(HttpMethod.Get, url);
}