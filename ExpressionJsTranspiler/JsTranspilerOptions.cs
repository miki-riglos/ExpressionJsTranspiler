using System.Linq.Expressions;

namespace ExpressionJsTranspiler;

public class BinaryFormat
{
	public string BeforeLeft { get; set; } = "(";
	public string AfterLeft { get; set; } = "";
	public string Operator { get; set; } = " {0} ";   // operator
	public string BeforeRight { get; set; } = "";
	public string AfterRight { get; set; } = ")";
}

public class JsTranspilerOptions
{
	public Dictionary<ExpressionType, string> Operators { get; set; } = new() {
		[ExpressionType.Not] = "!",
		[ExpressionType.Convert] = "",
		[ExpressionType.GreaterThan] = ">",
		[ExpressionType.GreaterThanOrEqual] = ">=",
		[ExpressionType.LessThan] = "<",
		[ExpressionType.LessThanOrEqual] = "<=",
		[ExpressionType.Equal] = "===",
		[ExpressionType.NotEqual] = "!==",
		[ExpressionType.AndAlso] = "&&",
		[ExpressionType.OrElse] = "||",
		[ExpressionType.Add] = "+",
		[ExpressionType.Subtract] = "-",
		[ExpressionType.Multiply] = "*",
		[ExpressionType.Divide] = "/"
	};

	public Dictionary<ExpressionType, BinaryFormat> BinaryFormats { get; set; } = new() {
		[ExpressionType.Multiply] = new BinaryFormat() {
			BeforeLeft = "((",
			AfterLeft = " * 100)",
			BeforeRight = "(",
			AfterRight = " * 100) / (100 * 100))"
		}
	};

	public Dictionary<Type, Func<object, string>> TypeConverters { get; protected set; } = new() {
		[typeof(string)] = value => $"'{value}'",
		[typeof(DateTime)] = value => $"new Date('{((DateTime)value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fff")}')",
		[typeof(bool)] = value => value.ToString()!.ToLower()
	};

	public Dictionary<string, string> NonExpressionMembers { get; protected set; } = new Dictionary<string, string>() {
		[$"{typeof(DateTime).Name}.{nameof(DateTime.Now)}"] = "new Date()",
		[$"{typeof(DateTime).Name}.{nameof(DateTime.Today)}"] = "(function() { var today = new Date(); today.setHours(0, 0, 0, 0); return today; })()",
	};

	public Dictionary<string, string> GenericICollectionProperties { get; set; } = new Dictionary<string, string>() {
		[nameof(ICollection<object>.Count)] = "length"
	};

	public Dictionary<string, string> EnumerableMethods { get; set; } = new Dictionary<string, string>() {
		[nameof(Enumerable.Count)] = "enumerable.count",
		[nameof(Enumerable.Any)] = "enumerable.any",
		[nameof(Enumerable.Select)] = "enumerable.select",
		[nameof(Enumerable.SelectMany)] = "enumerable.selectMany",
		[nameof(Enumerable.Sum)] = "enumerable.sum",
		[nameof(Enumerable.Average)] = "enumerable.average",
		[nameof(Enumerable.Min)] = "enumerable.min",
		[nameof(Enumerable.Max)] = "enumerable.max"
	};

	public string BodyFormat { get; protected set; } = "return {0};";        // body

	public string UnaryFormat { get; protected set; } = "{0}";              // operator

	public BinaryFormat BinaryFormat { get; protected set; } = new BinaryFormat();

	public BinaryFormat GetBinaryFormat(ExpressionType nodeType) {
		if (BinaryFormats.ContainsKey(nodeType)) {
			return BinaryFormats[nodeType];
		}
		return BinaryFormat;
	}

	public string MemberFormat { get; protected set; } = "{0}.{1}";         // parameter name, property name/path
	public string CollectionMemberFormat { get; protected set; } = "{0}.{1}";

	public string ParameterFormat { get; protected set; } = "{0}";          // parameter name

	public string ConstantFormat { get; protected set; } = "{0}";           // value or converted value

	public string ConditionalBeforeTestFormat { get; protected set; } = "(";
	public string ConditionalAfterTestFormat { get; protected set; } = " ? ";
	public string ConditionalAfterIfTrueFormat { get; protected set; } = " : ";
	public string ConditionalAfterIfFalseFormat { get; protected set; } = ")";

	public string StringFormatCallBeforeFormat { get; protected set; } = "(";
	public string StringFormatCallAfter1stLoopFormat { get; protected set; } = " + ";
	public string StringFormatCallNoMatchInLoopFormat { get; protected set; } = "'{0}'";  // string segment inside format template
	public string StringFormatCallAfterFormat { get; protected set; } = ")";

	public JsTranspilerOptions(params Action<JsTranspilerOptions>[] configureActions) {
		foreach (var configureAction in configureActions) {
			configureAction(this);
		}
	}

	// singletons
	public static JsTranspilerOptions Default = new();

	public static JsTranspilerOptions Template = new(
		to => to.TypeConverters[typeof(string)] = value => value.ToString()!,
		to => to.Operators[ExpressionType.Add] = "",
		to => to.BinaryFormats = new Dictionary<ExpressionType, BinaryFormat>() {
			[ExpressionType.Add] = new BinaryFormat() {
				BeforeLeft = "",
				AfterLeft = "",
				Operator = "",
				BeforeRight = "",
				AfterRight = ""
			}
		},
		to => to.BodyFormat = "{0}",
		to => to.MemberFormat = "${{{1}}}",

		to => to.StringFormatCallBeforeFormat = "",
		to => to.StringFormatCallAfter1stLoopFormat = "",
		to => to.StringFormatCallNoMatchInLoopFormat = "{0}",
		to => to.StringFormatCallAfterFormat = ""
	);

	public static JsTranspilerOptions ValueGetter = new(
		to => to.MemberFormat = "{0}.{1}.value()",
		to => to.CollectionMemberFormat = "{0}.{1}.getChildProperties.collection()"
	);
}
