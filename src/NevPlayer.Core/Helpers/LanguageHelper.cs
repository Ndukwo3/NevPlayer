using System.Globalization;

namespace NevPlayer.Core.Helpers
{
    public static class LanguageHelper
    {
        public static string GetFriendlyLanguageName(string isoCode)
        {
            if (string.IsNullOrWhiteSpace(isoCode)) return "Unknown";
            try
            {
                // Try getting culture name from ISO 639-1 or ISO 639-2 codes
                var culture = new CultureInfo(isoCode);
                return culture.DisplayName;
            }
            catch
            {
                // Hardcode fallbacks for common media language codes if CultureInfo fails
                var clean = isoCode.Trim().ToLowerInvariant();
                switch (clean)
                {
                    case "eng": case "en": return "English";
                    case "jpn": case "ja": return "Japanese";
                    case "spa": case "es": return "Spanish";
                    case "fre": case "fra": case "fr": return "French";
                    case "ger": case "deu": case "de": return "German";
                    case "chi": case "zho": case "zh": return "Chinese";
                    case "rus": case "ru": return "Russian";
                    case "kor": case "ko": return "Korean";
                    case "por": case "pt": return "Portuguese";
                    case "ita": case "it": return "Italian";
                    default: return isoCode; // Fallback to raw code
                }
            }
        }
    }
}
