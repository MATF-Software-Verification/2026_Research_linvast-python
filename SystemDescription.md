# Opis sistema: Python podrška za LINVAST

## 1. Imena autora

- Mihajlo Živković
- Svetozar Iković
- Petar Vuković

## 2. Opis problema

LINVAST prevodi izvorni kod različitih imperativnih jezika u zajednički AST
model. Takav model omogućava da se analiza i verifikacija programa rade nad
istom strukturom, nezavisno od konkretne sintakse ulaznog jezika.

Cilj ovog rada bio je dodavanje podrške za Python. To je podrazumevalo:

- prepoznavanje Python sintakse,
- povezivanje Python parsera sa LINVAST infrastrukturom,
- prevođenje Python konstrukcija u postojeće LINVAST čvorove,
- dodavanje novih čvorova za Python konstrukcije koje nisu bile pokrivene
  postojećim AST modelom,
- pokrivanje implementacije testovima.

Python se razlikuje od klasičnih C-like jezika po tome što nema eksplicitne
deklaracije promenljivih, dinamički je tipiziran, blokove određuje
uvučenošću i ima konstrukcije kao što su tuple unpacking, comprehension
izrazi, f-stringovi, `yield`, `await` i `match/case`.

Zadatak nije bio izvršavanje Python programa, već strukturno prevođenje
Python koda u LINVAST AST.

## 3. Opis arhitekture sistema

Python podrška je uklopljena u postojeću LINVAST arhitekturu.

### 3.1 LINVAST jezgro

Projekat `LINVAST` sadrži osnovne apstrakcije:

- `ASTNode`, baznu klasu svih AST čvorova,
- interfejse za AST buildere,
- registraciju buildera preko atributa,
- izuzetke za sintaksne i semantičke greške,
- kopiranje, zamenu podstabala, poređenje i JSON serijalizaciju.

Svi konkretni čvorovi nasleđuju `ASTNode` i čuvaju decu kroz listu
`Children`.

### 3.2 Imperativni AST model

Projekat `LINVAST.Imperative` sadrži zajedničke čvorove za imperativne
jezike:

- izraze: `IdNode`, `LitExprNode`, `ArithmExprNode`, `LogicExprNode`,
  `RelExprNode`, `FuncCallExprNode`, `AssignExprNode`,
- naredbe: `BlockStatNode`, `IfStatNode`, `WhileStatNode`, `ForStatNode`,
  `JumpStatNode`,
- deklaracije: `DeclStatNode`, `VarDeclNode`, `FuncNode`, `ClassNode`,
- pomoćne čvorove za operatore, import naredbe, nizove, rečnike i tagove.

Za Python su dodati ili prošireni čvorovi:

- `DeleteStatNode`, `GlobalStatNode`, `NonlocalStatNode`,
- `WithStatNode`, `TryStatNode`, `CatchClauseNode`,
- `AsyncStatNode`, `MatchStatNode`,
- `YieldExprNode`, `EllipsisLitExprNode`,
- `Pattern*Node` čvorovi za `match/case` obrasce.

### 3.3 Python parser

Python sintaksa se prepoznaje pomoću ANTLR parsera. Direktorijum
`LINVAST.Imperative/Builders/Python/ANTLR/` sadrži:

- `Python3Lexer.g4`,
- `Python3Parser.g4`,
- generisani C# lexer,
- generisani C# parser,
- generisane visitor klase.

ANTLR pravi Python parse tree. Taj parse tree se zatim prevodi u LINVAST AST.

### 3.4 PythonASTBuilder

`PythonASTBuilder` je centralni modul Python implementacije. On obilazi ANTLR
parse tree i pravi LINVAST čvorove.

Implementacija je podeljena na fajlove:

- `PythonASTBuilder.cs` - kreiranje parsera, obrada fajla i registracija za
  `.py` ekstenziju,
- `PythonASTBuilder.Expressions.cs` - izrazi, literali, pozivi, slicing,
  comprehension izrazi i f-stringovi,
- `PythonASTBuilder.Statements.cs` - blokovi, kontrola toka, `try`, `with`,
  `async` i `match/case`,
- `PythonASTBuilder.Declarations.cs` - importi, dodele, anotirane dodele,
  `del`, `global` i `nonlocal`,
- `PythonASTBuilder.Functions.cs` - funkcije, parametri, lambda izrazi,
  dekoratori i asinhrone funkcije,
- `PythonASTBuilder.Types.cs` - klase,
- `PythonASTBuilder.TupleUnpacking.cs` - tuple/list unpacking.

Builder je registrovan za Python fajlove atributom:

```csharp
[ASTBuilder(".py")]
```

## 4. Opis rešenja problema

Tok obrade Python programa je:

```text
Python kod
  -> Python3Lexer
  -> Python3Parser
  -> Python parse tree
  -> PythonASTBuilder
  -> LINVAST SourceNode
```

### 4.1 Osnovni algoritam

1. Python kod se prosleđuje ANTLR lexeru.
2. Lexer proizvodi tok tokena.
3. Parser pravi Python parse tree.
4. `PythonASTBuilder` obilazi parse tree.
5. Python pravila se prevode u LINVAST čvorove.
6. Na nivou fajla, funkcija i klasa dodatno se obrađuju deklaracije.
7. Rezultat je `SourceNode`, koren LINVAST AST-a.

### 4.2 Izrazi

Osnovni izrazi se prevode u postojeće LINVAST čvorove:

- identifikatori u `IdNode`,
- literali u `LitExprNode`,
- `None` u `NullLitExprNode`,
- aritmetički izrazi u `ArithmExprNode`,
- logički izrazi u `LogicExprNode`,
- poređenja u `RelExprNode`,
- pozivi funkcija u `FuncCallExprNode`,
- indeksiranje u `ArrAccessExprNode`.

Ulančana poređenja, kao `a < f() < b`, predstavljaju se kao kombinacija
relacionih i logičkih izraza. Srednji operand se kopira kao posebno podstablo
da bi svaki AST čvor imao tačno jednog roditelja.

### 4.3 Dodele i deklaracije

Pošto Python nema eksplicitne deklaracije promenljivih, builder prepoznaje
prvu dodelu promenljivoj i može je predstaviti kao `DeclStatNode`.

```python
x = 1
x = 2
```

Prva naredba može postati deklaracija, a druga ostaje obična dodela.

#### Dodele sa anotacijom

```python
x: int = 1
```

Prevodi se u deklaraciju sa tipom `int` i inicijalizatorom `1`.

### 4.4 Tuple/list unpacking

Za dodele oblika:

```python
a, b = 1, 2
a, *rest = 1, 2, 3, 4
```

builder izdvaja ciljeve sa leve strane, vrednosti sa desne strane i starred
target ako postoji. Kada je prevod bezbedan, formira više `VarDeclNode`
čvorova u jednoj deklaraciji. Ako se broj elemenata ne poklapa ili je
promenljiva već deklarisana, zadržava se obična dodela.

### 4.5 Funkcije i klase

Python funkcije se prevode u `FuncNode`. Parametri se čuvaju u
`FuncParamsNode`, tipovi iz anotacija u `DeclSpecsNode`, a podrazumevane
vrednosti kao inicijalizatori. Dekoratori se čuvaju kao `TagNode`, a
`async def` dobija tag `async`.

Python klase se prevode u `ClassNode`. Ime klase postaje deo `TypeDeclNode`,
bazne klase se čuvaju kao lista tipova, a telo klase kao skup deklaracija i
funkcija.

### 4.6 Kontrola toka

Kontrola toka se prevodi ovako:

- `if`, `elif`, `else` -> `IfStatNode`,
- `while` -> `WhileStatNode`,
- `for` -> `ForStatNode`,
- `try/except/else/finally` -> `TryStatNode` i `CatchClauseNode`,
- `with` -> `WithStatNode`,
- `async for`, `async with` -> `AsyncStatNode`.

Novi čvorovi su uvedeni tamo gde bi predstavljanje postojećim čvorovima
izgubilo bitne informacije o Python programu.

### 4.7 Sintetički pozivi

Konstrukcije bez direktnog LINVAST čvora predstavljene su sintetičkim pozivima
samo kada ih nije moguće prirodno izraziti postojećim čvorovima. Python
comprehension izrazi se spuštaju u inicijalizaciju akumulatora i ugnježdene
`ForStatNode`/`IfStatNode` čvorove. Kada se nalaze unutar većeg izraza,
builder ih podiže u prethodne naredbe sa privremenim akumulatorom, a u
originalnom izrazu ostaje običan `IdNode` tog akumulatora.

- slicing -> `slice(...)`,
- `await f()` -> `await(f())`,
- f-string -> `format(...)`.

Ovim se čuva struktura programa bez uvođenja posebnog čvora za svaku Python
specifičnu konstrukciju.

### 4.8 Pattern matching

Za `match/case` su uvedeni posebni čvorovi:

- `MatchStatNode`,
- `CaseNode`,
- `PatternLiteralNode`,
- `PatternCaptureNode`,
- `PatternWildcardNode`,
- `PatternOrNode`,
- `PatternAsNode`,
- `PatternSequenceNode`,
- `PatternMappingNode`,
- `PatternClassNode`.

Primer:

```python
match value:
    case [first, *rest] if first > 0:
        result = first
```

prevodi se u `MatchStatNode` sa `CaseNode` granom. Obrazac `[first, *rest]`
postaje `PatternSequenceNode`, `*rest` postaje `PatternStarNode`, a uslov
postaje guard izraz u `CaseNode`.
