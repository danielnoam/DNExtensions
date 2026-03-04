using System.Text.RegularExpressions;
using DNExtensions.Utilities;
using DNExtensions.Utilities.CustomFields;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "New FontStyle", menuName = "Scriptable Objects/Font Style", order = 1)]
public class SOFontStyle : ScriptableObject
{
    [Header("Format")]
    [SerializeField] private OptionalField<TMP_FontAsset> font = new OptionalField<TMP_FontAsset>(true);
    [SerializeField] private OptionalField<Color> fontColor = new OptionalField<Color>(Color.lightGreen, true);
    [SerializeField] private OptionalField<int> fontSizePercentage = new OptionalField<int>(85, true);
    [SerializeField] private OptionalField<HorizontalAlignmentOptions> alignment = new OptionalField<HorizontalAlignmentOptions>(HorizontalAlignmentOptions.Center, false);
    
    [Header("Styles")]
    [SerializeField] private bool bold;
    [SerializeField] private bool slant;
    [SerializeField] private bool strikeThrough;
    [SerializeField] private bool underline;

    public string ApplyStyle(string text)
    {
        var rich = text.Rich();
        
        if (fontColor.isSet) rich = rich.Color(fontColor.Value.ToHex());
        if (fontSizePercentage.isSet) rich = rich.Size($"{fontSizePercentage.Value}%");
        if (alignment.isSet) rich = rich.Align(alignment.Value.ToString().ToLower());
        if (bold) rich = rich.Bold();
        if (slant) rich = rich.Italic();
        if (strikeThrough) rich = rich.Strikethrough();
        if (underline) rich = rich.Underline();
        
        if (font.isSet && font.Value)
        {
            return rich.Font(font.Value.name).ToString();
        }
        
        return rich.ToString();
    }
    
    public string ApplyStyle(string text, char symbol)
    {
        if (string.IsNullOrEmpty(text)) return text;

        string pattern = $"{Regex.Escape(symbol.ToString())}(.*?){Regex.Escape(symbol.ToString())}";
        
        return Regex.Replace(text, pattern, match => 
        {
            string content = match.Groups[1].Value;
            return ApplyStyle(content);
        });
    }
}