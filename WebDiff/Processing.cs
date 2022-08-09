namespace WebDiff {
    static class Processing {
        public static string RemoveScripts(this string source) {
            while (true) {
                int start = source.IndexOf(scriptStart);
                if (start == -1) {
                    return source;
                }
                int end = source.IndexOf(scriptEnd, start) + scriptEnd.Length;
                if (end == -1) {
                    return source;
                }
                source = source[..start] + source[end..];
            }
        }

        const string scriptStart = "<script";
        const string scriptEnd = "</script>";
    }
}