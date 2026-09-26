namespace FinanceAI.Helpers;

public static class Glyph
{
    public const string Refresh = "\uE72C";
    public const string Chart = "\uE9D2";
    public const string Chat = "\uE8F2";
    public const string Add = "\uE710";
    public const string Up = "\uE74A";
    public const string Down = "\uE74B";
    public const string Edit = "\uE70F";
    public const string Delete = "\uE74D";
    public const string Save = "\uE74E";
    public const string Cancel = "\uE711";
    public const string List = "\uE8FD";
    public const string Star = "\uE734";
    public const string Search = "\uE721";
    public const string Calendar = "\uE787";
    public const string Home = "\uE80F";
    public const string Wallet = "\uE8C7";
}

public static class AppIcons
{
    public static string File(string glyph, Color color)
    {
        string name = Name(glyph);
        string tone = Tone(color);
        return $"ic_{name}_{tone}.png";
    }

    public static void Apply(Button button, string glyph, Color color)
    {
        button.ImageSource = File(glyph, color);
        button.ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Left, 8);
    }

    private static string Name(string glyph)
    {
        string code = glyph.Length == 1
            ? ((int)glyph[0]).ToString("X")
            : glyph.Trim().TrimStart('#').ToUpperInvariant();

        return code switch
        {
            "E72C" => "refresh",
            "E9D2" => "chart",
            "E8F2" => "chat",
            "E710" => "add",
            "E74A" => "up",
            "E74B" => "down",
            "E70F" => "edit",
            "E74D" => "delete",
            "E74E" => "save",
            "E711" => "cancel",
            "E8FD" => "list",
            "E734" => "star",
            "E721" => "search",
            "E787" => "calendar",
            "E80F" => "home",
            "E8C7" => "wallet",
            _ => "refresh"
        };
    }

    private static string Tone(Color color)
    {
        var hex = color.ToHex().ToUpperInvariant();
        if (hex.EndsWith("FFFFFF"))
            return "w";
        if (hex.EndsWith("D14343"))
            return "red";
        if (hex.EndsWith("10231C"))
            return "ink";
        return "accent";
    }
}

public class AppIconExtension : IMarkupExtension<ImageSource>
{
    public string Glyph { get; set; } = "";

    public string Tint { get; set; } = "#0E7C66";

    public ImageSource ProvideValue(IServiceProvider serviceProvider) =>
        AppIcons.File(Glyph, Color.FromArgb(Tint));

    object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) =>
        ProvideValue(serviceProvider);
}
