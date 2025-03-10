# ExpressionJsTranspiler

Converts a C# Lambda Expression into a Javascript function, using ExpressionVisitor class

```csharp
var jsTranspiler = new JsTranspiler(wh => wh.WarehouseId > 1 && wh.WarehouseId < 10);

var js = jsTranspiler.GetJs();
// => function(wh) { return ((wh.WarehouseId > 1) && (wh.WarehouseId < 10)); }

var parameters = jsTranspiler.Parameters;
// => [wh]

var body = jsTranspiler.Body;	        
// => ((wh.WarehouseId > 1) && (wh.WarehouseId < 10))
```

## Main Use Case
Define a validation rule as a C# lambda expression
```csharp
Expression<Func<Warehouse, bool>> isValidExpression = wh => wh.Volume > 10;
var jsTranspiler = new JsTranspiler(isValidExpression);
```
Compile it to be used in the server side
```csharp
// model
Warehouse warehouse = new Warehouse() { 
	Name = "Main",
	Volume = 20
};

// compile and validate
var isValid = isValidExpression.Compile();
Assert.IsTrue(isValid(warehouse));
```
Convert lambda expression to Javascript to be used in the client side.
```csharp
// get Function constructor arguments to pass to client
var jsFunctionCtorArgs = jsTranspiler.GetJsFunctionCtorArgs();
Assert.AreEqual(2, jsFunctionCtorArgs.Count);
Assert.AreEqual("wh", jsFunctionCtorArgs[0]);
Assert.AreEqual("return (wh.Volume > 10);", jsFunctionCtorArgs[1]);
```

```js
// model
let warehouse = { Name: "Main", Volume: 20 };

// instantiate Function in client side
let isValid = Function.apply(null, jsFunctionCtorArgs);
console.log(isValid(warehouse)); // => true
```