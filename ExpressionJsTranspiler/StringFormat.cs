using System.Text.RegularExpressions;

namespace ExpressionJsTranspiler;

public class Segment(bool isMatch, string value) {
	public bool IsMatch => isMatch;
	public string Value => value;
}

public class StringFormat
{
	// Segments in string Format template: "First value is {0}, second value is {1}"
	public static List<Segment> GetSegments(string template, string pattern = @"{(\d)}") {
		var segments = new List<Segment>();

		var re = new Regex(pattern);

		var lastIndex = 0;
		var counter = 0;
		string segmentValue;

		var result = re.Match(template);
		while (result.Success) {
			segmentValue = template.Substring(lastIndex, result.Index - lastIndex);
			if (segmentValue != string.Empty) {
				segments.Add(new Segment(false, segmentValue));
			}

			segments.Add(new Segment(true, result.Value));

			lastIndex = result.Index + result.Length;
			counter++;
			result = result.NextMatch();
		}

		segmentValue = template.Substring(lastIndex);
		if (segmentValue != string.Empty) {
			segments.Add(new Segment(false, segmentValue));
		}

		return segments;
	}
}
