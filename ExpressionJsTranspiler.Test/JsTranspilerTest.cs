using System.Linq.Expressions;

namespace ExpressionJsTranspiler.Test;

public enum Color
{
	Red,
	Blue
}

public class DefaultModelItem
{
	public DateTime CreatedAt { get; set; }
}

public class BusinessUnit : DefaultModelItem
{
	public int BusinessUnitId { get; set; }
	public string Name { get; set; } = null!;
	public bool Active { get; set; }
	public ICollection<Warehouse> Warehouses { get; set; } = new List<Warehouse>();
}

public class Warehouse : DefaultModelItem
{
	public int WarehouseId { get; set; }
	public string Name { get; set; } = null!;
	public BusinessUnit BusinessUnit { get; set; } = null!;
	public bool Active { get; set; }
	public Color Color { get; set; }
	public int Volume { get; set; }
}

[TestClass]
public class JsTranspilerTest
{
	[TestMethod]
	public void ValidationRuleTest() {
		Expression<Func<Warehouse, bool>> isValidExpression = wh => wh.Volume > 10;
		var jsTranspiler = new JsTranspiler(isValidExpression);

		// model
		Warehouse warehouse = new Warehouse() { 
			Name = "Main",
			Volume = 20
		};

		// compile and validate
		var isValid = isValidExpression.Compile();
		Assert.IsTrue(isValid(warehouse));

		// client side
		var jsFunctionCtorArgs = jsTranspiler.GetJsFunctionCtorArgs();
		Assert.AreEqual(2, jsFunctionCtorArgs.Count);
		Assert.AreEqual("wh", jsFunctionCtorArgs[0]);
		Assert.AreEqual("return (wh.Volume > 10);", jsFunctionCtorArgs[1]);
		// ... create function in JavaScript
		// var isValid = Function.apply(null, jsFunctionCtorArgs);
	}

	[TestMethod]
	public void PredicatesTest() {
		var predicates = new List<Expression<Func<Warehouse, bool>>>() {
			wh => wh.WarehouseId == 1,
			wh => wh.WarehouseId != 1,
			wh => wh.WarehouseId > 1 && wh.WarehouseId < 10,
			wh => wh.WarehouseId >= 1 && wh.WarehouseId <= 10,
			wh => wh.BusinessUnit.Name == "Main",
			wh => wh.WarehouseId == 1 && wh.BusinessUnit.Name == "Main",
			wh => (wh.WarehouseId == 1 || wh.BusinessUnit.Name == "Main") && wh.Name == "Main",
			wh => wh.Active == true,
			wh => wh.Active == false,
			wh => wh.Active,
			wh => !wh.Active,
			wh => !wh.BusinessUnit.Active,
			wh => wh.BusinessUnit.Warehouses.Count > 0
		};
		var expectedJsList = new List<string>() {
			"function(wh) { return (wh.WarehouseId === 1); }",
			"function(wh) { return (wh.WarehouseId !== 1); }",
			"function(wh) { return ((wh.WarehouseId > 1) && (wh.WarehouseId < 10)); }",
			"function(wh) { return ((wh.WarehouseId >= 1) && (wh.WarehouseId <= 10)); }",
			"function(wh) { return (wh.BusinessUnit.Name === 'Main'); }",
			"function(wh) { return ((wh.WarehouseId === 1) && (wh.BusinessUnit.Name === 'Main')); }",
			"function(wh) { return (((wh.WarehouseId === 1) || (wh.BusinessUnit.Name === 'Main')) && (wh.Name === 'Main')); }",
			"function(wh) { return (wh.Active === true); }",
			"function(wh) { return (wh.Active === false); }",
			"function(wh) { return wh.Active; }",
			"function(wh) { return !wh.Active; }",
			"function(wh) { return !wh.BusinessUnit.Active; }",
			"function(wh) { return (wh.BusinessUnit.Warehouses.length > 0); }",
		};

		foreach (var pair in predicates.Zip(expectedJsList, (predicate, expectedJs) => new { Predicate = predicate, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Predicate);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void CollectionsTest() {
		var predicates = new List<Expression<Func<BusinessUnit, object>>>() {
			bu => bu.Warehouses,
			bu => bu.Warehouses.Count > 0,
			bu => bu.Warehouses.Count() > 0,
			bu => bu.Warehouses.Count(wh => wh.Volume > 0),
			bu => bu.Warehouses.Count(wh => wh.Volume > 0) > 1,
			bu => bu.Warehouses.Any(),
			bu => bu.Warehouses.Any(wh => wh.Volume > 0),
			bu => bu.Warehouses.Select(wh => wh.Volume).Sum(),
			bu => bu.Warehouses.Distinct(),
			bu => bu.Warehouses.OrderBy(wh => wh.Name),
			bu => bu.Warehouses.OrderByDescending(wh => wh.Name),
		};
		var expectedJsList = new List<string>() {
			"function(bu) { return bu.Warehouses; }",
			"function(bu) { return (bu.Warehouses.length > 0); }",
			"function(bu) { return (enumerable.count(bu.Warehouses) > 0); }",
			"function(bu) { return enumerable.count(bu.Warehouses, function(wh) { return (wh.Volume > 0); }); }",
			"function(bu) { return (enumerable.count(bu.Warehouses, function(wh) { return (wh.Volume > 0); }) > 1); }",
			"function(bu) { return enumerable.any(bu.Warehouses); }",
			"function(bu) { return enumerable.any(bu.Warehouses, function(wh) { return (wh.Volume > 0); }); }",
			"function(bu) { return enumerable.sum(enumerable.select(bu.Warehouses, function(wh) { return wh.Volume; })); }",
			"function(bu) { return enumerable.distinct(bu.Warehouses); }",
			"function(bu) { return enumerable.orderBy(bu.Warehouses, function(wh) { return wh.Name; }); }",
			"function(bu) { return enumerable.orderByDescending(bu.Warehouses, function(wh) { return wh.Name; }); }"
		};

		foreach (var pair in predicates.Zip(expectedJsList, (predicate, expectedJs) => new { Predicate = predicate, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Predicate);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void ReturningStringTest() {
		var returnStringLambdas = new List<Expression<Func<Warehouse, string>>>() {
			wh => wh.Name,
			wh => wh.BusinessUnit.Name,
			wh => wh.Name + "-",
			wh => wh.Name + "-" + wh.BusinessUnit.Name,
			wh => string.Format("{0}-{1}", wh.Name, wh.BusinessUnit.Name),
			wh => string.Format(">{0}{1}<", wh.Name, wh.BusinessUnit.Name),
			wh => string.Format(">{0}{1}<", wh.Name, wh.BusinessUnit.Name) + "|",
			wh => $"{wh.Name}-{wh.BusinessUnit.Name}",
			wh => $"{wh.Name}-{wh.BusinessUnit.BusinessUnitId}-{wh.CreatedAt}-{wh.WarehouseId}",
			wh => $"{wh.Name}-{wh.CreatedAt}-{wh.WarehouseId}",
			wh => string.Format("{0}-{1}-{2}-{3}", wh.Name, wh.BusinessUnit.Name, wh.CreatedAt, wh.WarehouseId),
			wh => string.Format("{0}-{0}-{1}-{2}", wh.Name, wh.BusinessUnit.Name, wh.CreatedAt),
			wh => wh.WarehouseId > 1 ? wh.Name : wh.BusinessUnit.Name
		};
		var expectedJsList = new List<string>() {
			"function(wh) { return wh.Name; }",
			"function(wh) { return wh.BusinessUnit.Name; }",
			"function(wh) { return (wh.Name + '-'); }",
			"function(wh) { return ((wh.Name + '-') + wh.BusinessUnit.Name); }",
			"function(wh) { return (wh.Name + '-' + wh.BusinessUnit.Name); }",
			"function(wh) { return ('>' + wh.Name + wh.BusinessUnit.Name + '<'); }",
			"function(wh) { return (('>' + wh.Name + wh.BusinessUnit.Name + '<') + '|'); }",
			"function(wh) { return (wh.Name + '-' + wh.BusinessUnit.Name); }",
			"function(wh) { return (wh.Name + '-' + wh.BusinessUnit.BusinessUnitId + '-' + wh.CreatedAt + '-' + wh.WarehouseId); }",
			"function(wh) { return (wh.Name + '-' + wh.CreatedAt + '-' + wh.WarehouseId); }",
			"function(wh) { return (wh.Name + '-' + wh.BusinessUnit.Name + '-' + wh.CreatedAt + '-' + wh.WarehouseId); }",
			"function(wh) { return (wh.Name + '-' + wh.Name + '-' + wh.BusinessUnit.Name + '-' + wh.CreatedAt); }",
			"function(wh) { return ((wh.WarehouseId > 1) ? wh.Name : wh.BusinessUnit.Name); }"
		};

		foreach (var pair in returnStringLambdas.Zip(expectedJsList, (lambda, expectedJs) => new { Lambda = lambda, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Lambda);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void LambdaExpresionsTest() {
		var expressions = new List<Expression<Func<Warehouse, object>>>() {
			wh => wh.BusinessUnit.Name,
			wh => wh.CreatedAt,
			wh => true,
			wh => false,
			wh => 1,
			wh => DateTime.Now,
			wh => DateTime.Today,
			wh => wh.CreatedAt > DateTime.Now,
			wh => wh.CreatedAt > DateTime.Today,
			wh => wh.Color == Color.Red,
			wh => wh.Color == Color.Blue,
			wh => wh == null || wh.Color == Color.Blue
		};
		var expectedJsList = new List<string>() {
			"function(wh) { return wh.BusinessUnit.Name; }",
			"function(wh) { return wh.CreatedAt; }",
			"function(wh) { return true; }",
			"function(wh) { return false; }",
			"function(wh) { return 1; }",
			"function(wh) { return new Date(); }",
			"function(wh) { return (function() { var today = new Date(); today.setHours(0, 0, 0, 0); return today; })(); }",
			"function(wh) { return (wh.CreatedAt > new Date()); }",
			"function(wh) { return (wh.CreatedAt > (function() { var today = new Date(); today.setHours(0, 0, 0, 0); return today; })()); }",
			"function(wh) { return (wh.Color === 0); }",
			"function(wh) { return (wh.Color === 1); }",
			"function(wh) { return ((wh === null) || (wh.Color === 1)); }"
		};

		foreach (var pair in expressions.Zip(expectedJsList, (expression, expectedJs) => new { Expression = expression, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Expression);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void TemplateOptionTest() {
		var returnStringLambdas = new List<Expression<Func<Warehouse, string>>>() {
			wh => wh.Name,
			wh => wh.BusinessUnit.Name,
			wh => wh.Name + "-",
			wh => wh.Name + "-" + wh.BusinessUnit.Name,
			wh => string.Format("{0}-{1}", wh.Name, wh.BusinessUnit.Name),
			wh => string.Format(">{0}{1}<", wh.Name, wh.BusinessUnit.Name),
			wh => string.Format(">{0}{1}<", wh.Name, wh.BusinessUnit.Name) + "|",
			wh => $"{wh.Name}-{wh.BusinessUnit.Name}",
			wh => $"{wh.Name}-{wh.BusinessUnit.BusinessUnitId}-{wh.CreatedAt}-{wh.WarehouseId}",
			wh => $"{wh.Name}-{wh.CreatedAt}-{wh.WarehouseId}",
			wh => string.Format("{0}-{1}-{2}-{3}", wh.Name, wh.BusinessUnit.Name, wh.CreatedAt, wh.WarehouseId),
			wh => string.Format("{0}-{0}-{1}-{2}", wh.Name, wh.BusinessUnit.Name, wh.CreatedAt),
			wh => wh.Color.ToString(),
			wh => wh.Color + "-" + wh.BusinessUnit.Name,
			wh => string.Format(">{0}{1}<", wh.Color, wh.BusinessUnit.Name) + "|",
			wh => $"{wh.Color}-{wh.CreatedAt}-{wh.WarehouseId}",
			wh => wh.WarehouseId > 1 ? wh.Name : wh.BusinessUnit.Name
		};
		var expectedTemplates = new List<string>() {
			"${Name}",
			"${BusinessUnit.Name}",
			"${Name}-",
			"${Name}-${BusinessUnit.Name}",
			"${Name}-${BusinessUnit.Name}",
			">${Name}${BusinessUnit.Name}<",
			">${Name}${BusinessUnit.Name}<|",
			"${Name}-${BusinessUnit.Name}",
			"${Name}-${BusinessUnit.BusinessUnitId}-${CreatedAt}-${WarehouseId}",
			"${Name}-${CreatedAt}-${WarehouseId}",
			"${Name}-${BusinessUnit.Name}-${CreatedAt}-${WarehouseId}",
			"${Name}-${Name}-${BusinessUnit.Name}-${CreatedAt}",
			"${Color}",
			"${Color}-${BusinessUnit.Name}",
			">${Color}${BusinessUnit.Name}<|",
			"${Color}-${CreatedAt}-${WarehouseId}",
			"((${WarehouseId} > 1) ? ${Name} : ${BusinessUnit.Name})"   // will not be evaluated in client
            };

		foreach (var pair in returnStringLambdas.Zip(expectedTemplates, (lambda, expectedTemplate) => new { Lambda = lambda, ExpectedTemplate = expectedTemplate })) {
			var actualTemplate = JsTranspiler.GetTemplate(pair.Lambda);
			Assert.AreEqual(pair.ExpectedTemplate, actualTemplate);
		}
	}

	[TestMethod]
	public void ValueGetterOptionTest() {
		var expressions = new List<Expression<Func<Warehouse, object>>>() {
			wh => wh.WarehouseId,
			wh => wh.CreatedAt,
			wh => wh.WarehouseId * 1,
			wh => wh.WarehouseId * wh.BusinessUnit.BusinessUnitId,
			wh => wh.WarehouseId > 1 ? wh.Name : "Zero",
			wh => wh.WarehouseId > 1 ? wh.Name : $"{wh.Name} is Zero"
		};
		var expectedJsList = new List<string>() {
			"function(wh) { return wh.WarehouseId.value(); }",
			"function(wh) { return wh.CreatedAt.value(); }",
			"function(wh) { return ((wh.WarehouseId.value() * 100) * (1 * 100) / (100 * 100)); }",
			"function(wh) { return ((wh.WarehouseId.value() * 100) * (wh.BusinessUnit.BusinessUnitId.value() * 100) / (100 * 100)); }",
			"function(wh) { return ((wh.WarehouseId.value() > 1) ? wh.Name.value() : 'Zero'); }",
			"function(wh) { return ((wh.WarehouseId.value() > 1) ? wh.Name.value() : (wh.Name.value() + ' is Zero')); }"
		};

		foreach (var pair in expressions.Zip(expectedJsList, (expression, expectedJs) => new { Expression = expression, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJsWithValueGetter(pair.Expression);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void CollectionsValueGetterOptionTest() {
		var expressions = new List<Expression<Func<BusinessUnit, object>>>() {
			bu => bu.Warehouses.Select(wh => wh.Volume),
			bu => bu.Warehouses.Select(wh => wh.Volume).Sum(),
		};
		var expectedJsList = new List<string>() {
			"function(bu) { return enumerable.select(bu.Warehouses.getChildProperties.collection(), function(wh) { return wh.Volume.value(); }); }",
			"function(bu) { return enumerable.sum(enumerable.select(bu.Warehouses.getChildProperties.collection(), function(wh) { return wh.Volume.value(); })); }"
		};

		foreach (var pair in expressions.Zip(expectedJsList, (expression, expectedJs) => new { Expression = expression, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJsWithValueGetter(pair.Expression);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void ModelItemTypeTest() {
		var expressions = new List<Expression<Func<BusinessUnit, bool>>>() {
			bu => bu.GetType() == typeof(BusinessUnit),
			bu => bu is BusinessUnit
		};
		var expectedJsList = new List<string>() {
			"function(bu) { return (bu.__modelName === 'BusinessUnit'); }",
			"function(bu) { return (bu.__modelName === 'BusinessUnit'); }"
		};

		foreach (var pair in expressions.Zip(expectedJsList, (expression, expectedJs) => new { Expression = expression, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Expression);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}

	[TestMethod]
	public void ModelItemCastTest() {
		var expressions = new List<Expression<Func<DefaultModelItem, object>>>() {
			dmi => (dmi as BusinessUnit)!.BusinessUnitId,
			dmi => ((BusinessUnit)dmi).BusinessUnitId,
		};
		var expectedJsList = new List<string>() {
			"function(dmi) { return dmi?.BusinessUnitId; }",
			"function(dmi) { return dmi?.BusinessUnitId; }"
		};

		foreach (var pair in expressions.Zip(expectedJsList, (expression, expectedJs) => new { Expression = expression, ExpectedJs = expectedJs })) {
			var actualJs = JsTranspiler.GetJs(pair.Expression);
			Assert.AreEqual(pair.ExpectedJs, actualJs);
		}
	}
}
