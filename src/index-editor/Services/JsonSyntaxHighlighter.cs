using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace IndexEditor.Services;

/// <summary>
/// Provides lightweight JSON syntax highlighting for TextBlocks using Inlines.
/// </summary>
public static class JsonSyntaxHighlighter
{
    private static readonly SolidColorBrush StringBrush = new SolidColorBrush(Color.Parse("#D14")); // Red-ish for strings
    private static readonly SolidColorBrush NumberBrush = new SolidColorBrush(Color.Parse("#09885A")); // Green for numbers
    private static readonly SolidColorBrush KeyBrush = new SolidColorBrush(Color.Parse("#0451A5")); // Blue for keys
    private static readonly SolidColorBrush BoolNullBrush = new SolidColorBrush(Color.Parse("#0000FF")); // Blue for booleans/null
    private static readonly SolidColorBrush PunctuationBrush = new SolidColorBrush(Color.Parse("#000000")); // Black for punctuation
    private static readonly SolidColorBrush DefaultBrush = new SolidColorBrush(Color.Parse("#000000")); // Black default

    /// <summary>
    /// Applies JSON syntax highlighting to a TextBlock by setting its Inlines.
    /// </summary>
    public static void ApplyHighlighting(TextBlock textBlock, string jsonText)
    {
        if (textBlock == null || string.IsNullOrEmpty(jsonText))
            return;

        textBlock.Inlines.Clear();
        
        var tokens = Tokenize(jsonText);
        
        foreach (var (text, type) in tokens)
        {
            var run = new Run(text)
            {
                Foreground = GetBrushForTokenType(type)
            };
            textBlock.Inlines.Add(run);
        }
    }

    private static SolidColorBrush GetBrushForTokenType(TokenType type)
    {
        return type switch
        {
            TokenType.String => StringBrush,
            TokenType.Number => NumberBrush,
            TokenType.Key => KeyBrush,
            TokenType.BooleanOrNull => BoolNullBrush,
            TokenType.Punctuation => PunctuationBrush,
            _ => DefaultBrush
        };
    }

    private static List<(string text, TokenType type)> Tokenize(string jsonText)
    {
        var tokens = new List<(string, TokenType)>();
        var i = 0;
        var length = jsonText.Length;
        var inString = false;
        var isKey = false; // Track if we're expecting a key (after { or ,)
        var afterColon = false; // Track if we just saw a colon

        while (i < length)
        {
            var ch = jsonText[i];

            // Handle strings
            if (ch == '"' && (i == 0 || jsonText[i - 1] != '\\'))
            {
                var startPos = i;
                i++; // Skip opening quote
                
                // Find closing quote
                while (i < length)
                {
                    if (jsonText[i] == '"' && jsonText[i - 1] != '\\')
                    {
                        i++; // Include closing quote
                        break;
                    }
                    i++;
                }
                
                var stringValue = jsonText.Substring(startPos, i - startPos);
                
                // Determine if this is a key or a value
                // Keys come after { or , and before :
                // Look ahead to see if there's a colon
                var lookAhead = i;
                while (lookAhead < length && char.IsWhiteSpace(jsonText[lookAhead]))
                    lookAhead++;
                
                var isThisAKey = lookAhead < length && jsonText[lookAhead] == ':';
                
                tokens.Add((stringValue, isThisAKey ? TokenType.Key : TokenType.String));
                continue;
            }

            // Handle numbers
            if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(jsonText[i + 1])))
            {
                var startPos = i;
                
                if (ch == '-') i++; // Handle negative
                
                while (i < length && (char.IsDigit(jsonText[i]) || jsonText[i] == '.' || jsonText[i] == 'e' || jsonText[i] == 'E' || jsonText[i] == '+' || jsonText[i] == '-'))
                {
                    i++;
                }
                
                tokens.Add((jsonText.Substring(startPos, i - startPos), TokenType.Number));
                continue;
            }

            // Handle boolean and null
            if (i + 4 <= length && jsonText.Substring(i, 4) == "true")
            {
                tokens.Add(("true", TokenType.BooleanOrNull));
                i += 4;
                continue;
            }
            
            if (i + 5 <= length && jsonText.Substring(i, 5) == "false")
            {
                tokens.Add(("false", TokenType.BooleanOrNull));
                i += 5;
                continue;
            }
            
            if (i + 4 <= length && jsonText.Substring(i, 4) == "null")
            {
                tokens.Add(("null", TokenType.BooleanOrNull));
                i += 4;
                continue;
            }

            // Handle punctuation and whitespace
            if (ch == '{' || ch == '}' || ch == '[' || ch == ']' || ch == ',' || ch == ':')
            {
                tokens.Add((ch.ToString(), TokenType.Punctuation));
                i++;
                continue;
            }

            // Handle whitespace (preserve it)
            if (char.IsWhiteSpace(ch))
            {
                var startPos = i;
                while (i < length && char.IsWhiteSpace(jsonText[i]))
                {
                    i++;
                }
                tokens.Add((jsonText.Substring(startPos, i - startPos), TokenType.Whitespace));
                continue;
            }

            // Unknown character - just add it
            tokens.Add((ch.ToString(), TokenType.Default));
            i++;
        }

        return tokens;
    }

    private enum TokenType
    {
        Default,
        String,
        Number,
        Key,
        BooleanOrNull,
        Punctuation,
        Whitespace
    }
}

