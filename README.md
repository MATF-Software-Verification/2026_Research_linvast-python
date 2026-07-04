# LINVAST Python podrška

Python podrška proširuje LINVAST novim builderom koji prevodi Python izvorni
kod u zajednički LINVAST AST model.

## Autori

- Mihajlo Živković
- Svetozar Iković
- Petar Vuković

## Prevođenje i pokretanje

Komande se pokreću iz korena repozitorijuma.

Potrebni alati:

- .NET SDK sa podrškom za `net5.0` i `netstandard2.1`
- Git
- Docker, opciono, za pokretanje testova bez lokalno instaliranog .NET SDK-a

Glavne NuGet zavisnosti:

- `Antlr4.Runtime.Standard`
- `Newtonsoft.Json`
- `Serilog`
- `NUnit`
- `NUnit3TestAdapter`
- `Microsoft.NET.Test.Sdk`
- `MathNet.Symbolics`

ANTLR C# fajlovi za Python parser su već generisani u direktorijumu
`LINVAST.Imperative/Builders/Python/ANTLR`.

Prevođenje projekta:

```bash
dotnet restore LINVAST.sln
dotnet build LINVAST.sln
```

Release build:

```bash
dotnet build LINVAST.sln -c Release
```

## Korišćenje

Python builder je registrovan za `.py` fajlove, pa se može koristiti preko
`ImperativeASTFactory`.

```csharp
using System;
using LINVAST.Imperative;
using LINVAST.Nodes;

ASTNode ast = new ImperativeASTFactory().BuildFromFile("sample.py");

Console.WriteLine(ast.GetText());
Console.WriteLine(ast.ToJson(compact: false));
```

Builder se može koristiti i direktno:

```csharp
using System;
using LINVAST.Imperative.Builders.Python;

var builder = new PythonASTBuilder();
var source = "def square(x: int) -> int:\n    return x * x\n";
var ast = builder.BuildFromSource(source);

Console.WriteLine(ast.ToJson(compact: false));
```

Rezultat je LINVAST `SourceNode`, koji se dalje može obilaziti, porediti,
kopirati ili serijalizovati u JSON.

## Ulazni primeri

### Deklaracije i tuple unpacking

```python
count: int = 1
a, *rest = 1, 2, 3, 4
count = count + 1
```

### Funkcije, anotacije i dekoratori

```python
@trace
def add(x: int, y: int = 0) -> int:
    return x + y
```

### Kontrola toka

```python
try:
    value = load()
except ValueError as error:
    value = 0
else:
    value = value + 1
finally:
    cleanup()
```

### Pattern matching

```python
match value:
    case [first, *rest] if first > 0:
        result = first
    case _:
        result = 0
```

### Comprehension i f-string

```python
squares = {x: x ** 2 for x in range(1, 6)}
message = f"count={len(squares)}"
```

## Alati korišćeni za analizu

- ANTLR4 Python gramatika i generisani C# lexer/parser
- .NET compiler/MSBuild
- NUnit
- `Microsoft.NET.Test.Sdk`
- Docker za izolovano pokretanje testova
- Git diff i code review

## Testovi

Python testovi se nalaze u `LINVAST.Tests/Imperative/Builders/Python/`:

- `ExpressionTests.cs`
- `DeclarationTests.cs`
- `FunctionTests.cs`
- `ClassTests.cs`
- `ControlFlowTests.cs`
- `PatternTests.cs`
- `ImportTests.cs`
- `BlockTests.cs`
- `BuildingErrorTests.cs`

Pokretanje svih testova:

```bash
dotnet test LINVAST.sln
```

Pokretanje samo Python testova:

```bash
dotnet test LINVAST.Tests/LINVAST.Tests.csproj --filter "FullyQualifiedName~Python"
```

Pokretanje samo Python testova u Docker kontejneru:

```bash
docker run --rm -v "$PWD":/workspace -v /tmp/linvast-nuget:/root/.nuget/packages -w /workspace mcr.microsoft.com/dotnet/sdk:5.0 dotnet test LINVAST.Tests/LINVAST.Tests.csproj --nologo --filter "FullyQualifiedName~Python"
```

Pokretanje svih testova u Docker kontejneru:

```bash
docker run --rm -v "$PWD":/workspace -v /tmp/linvast-nuget:/root/.nuget/packages -w /workspace mcr.microsoft.com/dotnet/sdk:5.0 dotnet test LINVAST.Tests/LINVAST.Tests.csproj --nologo
```
