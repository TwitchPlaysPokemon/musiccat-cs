using System;

namespace MusicCat;

public static class StringExtensions
{
	//https://social.technet.microsoft.com/wiki/contents/articles/26805.c-calculating-percentage-similarity-of-2-strings.aspx
	public static float LevenshteinRatio(this string source, string target)
	{
		if (source == null || target == null) return 0.0f;
		if (source.Length == 0 || target.Length == 0) return 0.0f;
		if (source == target) return 1.0f;

		int sourceWordCount = source.Length;
		int targetWordCount = target.Length;

		int[,] distance = new int[sourceWordCount + 1, targetWordCount + 1];

		// Step 2
		for (int i = 0; i <= sourceWordCount; distance[i, 0] = i++) ;
		for (int j = 0; j <= targetWordCount; distance[0, j] = j++) ;

		for (int i = 1; i <= sourceWordCount; i++)
		{
			for (int j = 1; j <= targetWordCount; j++)
			{
				// Step 3
				int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;

				// Step 4
				distance[i, j] = Math.Min(Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1), distance[i - 1, j - 1] + cost);
			}
		}
		return 1.0f - (float)distance[sourceWordCount, targetWordCount] / Math.Max(source.Length, target.Length);
	}
}