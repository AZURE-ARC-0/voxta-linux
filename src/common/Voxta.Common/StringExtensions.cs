namespace System;

public static class StringExtensions
{
    public static string TrimExcess(this string value)
    {
        return value.Trim(' ', '\r', '\n');
    }

    public static string TrimCopyPasteArtefacts(this string value)
    {
        return value.Trim('"', ' ');
    }
    
    public static string TrimContainerAndToLower(this string value)
    {
        return value.Trim('\'', '"', '.', '[', ']', ' ', '\r', '\n').ToLowerInvariant();
    }

    // https://gist.github.com/Davidblkx/e12ab0bb2aff7fd8072632b396538560
    public static int GetLevenshteinDistance(this string source, string value)
    {
        var sourceLength = source.Length;
        var valueLength = value.Length;

        var matrix = new int[sourceLength + 1, valueLength + 1];

        // First calculation, if one entry is empty return full length
        if (sourceLength == 0)
            return valueLength;

        if (valueLength == 0)
            return sourceLength;

        // Initialization of matrix with row size source1Length and columns size source2Length
        for (var i = 0; i <= sourceLength; matrix[i, 0] = i++)
        {
        }

        for (var j = 0; j <= valueLength; matrix[0, j] = j++)
        {
        }

        // Calculate rows and column distances
        for (var i = 1; i <= sourceLength; i++)
        {
            for (var j = 1; j <= valueLength; j++)
            {
                var cost = (value[j - 1] == source[i - 1]) ? 0 : 1;

                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        // return result
        return matrix[sourceLength, valueLength];
    }
}