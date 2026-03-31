# LINVAST - Language-INVariant AST library
[![Issues](https://img.shields.io/github/issues/LINVAST/LINVAST.svg)](https://github.com/LINVAST/LINVAST/issues)
[![Stable release](https://img.shields.io/github/release/LINVAST/LINVAST.svg?label=stable)](https://github.com/LINVAST/LINVAST/releases)
[![Latest release](https://img.shields.io/github/tag-pre/LINVAST/LINVAST.svg?label=latest)](https://github.com/LINVAST/LINVAST/releases)
[![NuGet](https://img.shields.io/nuget/vpre/LINVAST.svg?label=NuGet:%20LINVAST)](https://nuget.org/packages/LINVAST)
[![NuGet](https://img.shields.io/nuget/vpre/LINVAST.Imperative.svg?label=NuGet:%20LINVAST.Imperative)](https://nuget.org/packages/LINVAST.Imperative)
[![Stable release](https://img.shields.io/github/release/LINVAST/CLI.svg?label=linvast-cli)](https://github.com/LINVAST/CLI/releases)

LINVAST is a set of libraries that provide a common language-invariant AST API for different programming languages by abstracting [ANTLR](https://www.antlr.org/) parse trees. Currently, the main focus of the project is the imperative programming paradigm, with supported languages:
- `C` (almost complete support)
- `Java` (partial support, pendingn development)
- `Go` (partial support, pending development)
- `Lua` (partial support, pending development)

## Motivation and project description
> *There are many programming languages out there and, even though their syntax might be different, they often derive from or use certain universal programming concepts. We also call that a _way of writing code_ or, more commonly, a programming paradigm. The motivation for LINVAST came from the inability to find a shared API for every programming language that is a part of a procedural paradigm. LINVAST aims to create a common abstraction for imperative (but also procedural, OO, script and, through a few concepts, functional) programming paradigm so that it is possible to view many different programming languages on the same level of abstraction.*
>
> *LINVAST can theoretically work with any programming language as long as the adapter for that language is written (hence, _Language Invariant_). Adapters (or, in further text, _Builders_) serve as an intermediary between ANTLR parse trees and language-invariant ASTs. Builders are used to generate AST from a parse tree and they are implemented differently for every programming language due to native differences in parse trees. LINVAST provides intuitive API for generating and traversing generated ASTs and also provides already implemented visitors for common AST operations such as expression evaluation.*
>
> *LINVAST was made as a proof of concept for my MSc thesis (_Semantic comparison of structurally similar imperative code segments_) but has grown with aim to become fully operational and manageable long-term. LINVAST can be used as a tool to generate serialized AST in JSON, and provides intuitive API which is used in many standalone programs which use LINVAST API to visualize or compare generated ASTs.*

## Examples

### Using LINVAST in your own codebase

```cs
// Creating AST
LINVAST.ASTNode ast = new LINVAST.Imperative.ImperativeASTFactory().BuildFromFile("my_src_path");

// Copying AST - AST is immutable
LINVAST.ASTNode copy = ast.Copy();

// Common operators are overridden
if (ast == copy) {
	// Prints code in C-like syntax
	Console.WriteLine(ast.GetText());
}

// Each AST node has direct children property
LINVAST.ASTNode child = ast.Children[1];
LINVAST.ASTNode replacement = copy.Children[2];

// Substitution, returns new AST
LINVAST.ASTNode subCopy = copy.Substitute(child, replacement);

// JSON serialization
string json = ast.ToJson(compact: false);

// Views - safe cast which throws when cast can't be performed
using LINVAST.Imperative.Nodes;
BinaryExprNode expr = ast.Children[4].As<BinaryExprNode>();

// Specific nodes have useful and intuitive properties
int line = expr.Line
ExprNode left = expr.LeftOperand;
OpNode op = expr.Operator;
ExprNode right = expr.RightOperand;

// Check specific node types
if (expr is AssignExprNode aexpr) {
	var simplifiedExpr = aexpr.SimplifyComplexAssignment();
	// ...
}

// Use provided visitors to perform logic
using LINVAST.Imperative.Visitors;
var res = ConstantExpressionEvaluator.TryEvaluateAs<int>(expr);

// Or implement your own custom visitor...
class MyVisitor<int> : BaseASTVisitor<int>
{
	public override int Visit(ExprNode node) {
		// ...
	}

	// ...
}
```

### Language-invariant AST examples

Concrete examples on how to use LINVAST as a standalone CLI tool can be seen [here](https://github.com/LINVAST/CLI/).

#### AST generated from C source:
```c
int gl_y = 2;
void f(int x);
int gl_x = 3;

int main()
{
	int x = 1;
	//printf("Hello world!% d\n", x);
	return 0;
}

static int gl_z;
```

<details>
<summary>Click to expand!</summary>

```json
{
  "Name": null,
  "NodeType": "SourceNode",
  "Line": 1,
  "Children": [
    {
      "NodeType": "DeclStatNode",
      "Line": 1,
      "Children": [
        {
          "Modifiers": {
            "AccessModifiers": "Unspecified",
            "QualifierFlags": "None"
          },
          "TypeName": "int",
          "NodeType": "DeclSpecsNode",
          "Line": 1,
          "Children": []
        },
        {
          "NodeType": "DeclListNode",
          "Line": 1,
          "Children": [
            {
              "Pointer": false,
              "NodeType": "VarDeclNode",
              "Line": 1,
              "Children": [
                {
                  "Identifier": "gl_y",
                  "NodeType": "IdNode",
                  "Line": 1,
                  "Children": []
                },
                {
                  "Value": 2,
                  "Suffix": null,
                  "TypeCode": "Int32",
                  "NodeType": "LitExprNode",
                  "Line": 1,
                  "Children": []
                }
              ]
            }
          ]
        }
      ]
    },
    {
      "NodeType": "DeclStatNode",
      "Line": 3,
      "Children": [
        {
          "Modifiers": {
            "AccessModifiers": "Unspecified",
            "QualifierFlags": "None"
          },
          "TypeName": "void",
          "NodeType": "DeclSpecsNode",
          "Line": 3,
          "Children": []
        },
        {
          "NodeType": "DeclListNode",
          "Line": 3,
          "Children": [
            {
              "Pointer": false,
              "NodeType": "FuncDeclNode",
              "Line": 3,
              "Children": [
                {
                  "Identifier": "f",
                  "NodeType": "IdNode",
                  "Line": 3,
                  "Children": []
                },
                {
                  "IsVariadic": false,
                  "NodeType": "FuncParamsNode",
                  "Line": 3,
                  "Children": [
                    {
                      "NodeType": "FuncParamNode",
                      "Line": 3,
                      "Children": [
                        {
                          "Modifiers": {
                            "AccessModifiers": "Unspecified",
                            "QualifierFlags": "None"
                          },
                          "TypeName": "int",
                          "NodeType": "DeclSpecsNode",
                          "Line": 3,
                          "Children": []
                        },
                        {
                          "Pointer": false,
                          "NodeType": "VarDeclNode",
                          "Line": 3,
                          "Children": [
                            {
                              "Identifier": "x",
                              "NodeType": "IdNode",
                              "Line": 3,
                              "Children": []
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    },
    {
      "NodeType": "DeclStatNode",
      "Line": 5,
      "Children": [
        {
          "Modifiers": {
            "AccessModifiers": "Unspecified",
            "QualifierFlags": "None"
          },
          "TypeName": "int",
          "NodeType": "DeclSpecsNode",
          "Line": 5,
          "Children": []
        },
        {
          "NodeType": "DeclListNode",
          "Line": 5,
          "Children": [
            {
              "Pointer": false,
              "NodeType": "VarDeclNode",
              "Line": 5,
              "Children": [
                {
                  "Identifier": "gl_x",
                  "NodeType": "IdNode",
                  "Line": 5,
                  "Children": []
                },
                {
                  "Value": 3,
                  "Suffix": null,
                  "TypeCode": "Int32",
                  "NodeType": "LitExprNode",
                  "Line": 5,
                  "Children": []
                }
              ]
            }
          ]
        }
      ]
    },
    {
      "NodeType": "FuncDefNode",
      "Line": 8,
      "Children": [
        {
          "Modifiers": {
            "AccessModifiers": "Unspecified",
            "QualifierFlags": "None"
          },
          "TypeName": "int",
          "NodeType": "DeclSpecsNode",
          "Line": 8,
          "Children": []
        },
        {
          "Pointer": false,
          "NodeType": "FuncDeclNode",
          "Line": 8,
          "Children": [
            {
              "Identifier": "main",
              "NodeType": "IdNode",
              "Line": 8,
              "Children": []
            }
          ]
        },
        {
          "NodeType": "BlockStatNode",
          "Line": 10,
          "Children": [
            {
              "NodeType": "DeclStatNode",
              "Line": 10,
              "Children": [
                {
                  "Modifiers": {
                    "AccessModifiers": "Unspecified",
                    "QualifierFlags": "None"
                  },
                  "TypeName": "int",
                  "NodeType": "DeclSpecsNode",
                  "Line": 10,
                  "Children": []
                },
                {
                  "NodeType": "DeclListNode",
                  "Line": 10,
                  "Children": [
                    {
                      "Pointer": false,
                      "NodeType": "VarDeclNode",
                      "Line": 10,
                      "Children": [
                        {
                          "Identifier": "x",
                          "NodeType": "IdNode",
                          "Line": 10,
                          "Children": []
                        },
                        {
                          "Value": 1,
                          "Suffix": null,
                          "TypeCode": "Int32",
                          "NodeType": "LitExprNode",
                          "Line": 10,
                          "Children": []
                        }
                      ]
                    }
                  ]
                }
              ]
            },
            {
              "Type": "Return",
              "NodeType": "JumpStatNode",
              "Line": 12,
              "Children": [
                {
                  "Value": 0,
                  "Suffix": null,
                  "TypeCode": "Int32",
                  "NodeType": "LitExprNode",
                  "Line": 12,
                  "Children": []
                }
              ]
            }
          ]
        }
      ]
    },
    {
      "NodeType": "DeclStatNode",
      "Line": 15,
      "Children": [
        {
          "Modifiers": {
            "AccessModifiers": "Unspecified",
            "QualifierFlags": "Static"
          },
          "TypeName": "int",
          "NodeType": "DeclSpecsNode",
          "Line": 15,
          "Children": []
        },
        {
          "NodeType": "DeclListNode",
          "Line": 15,
          "Children": [
            {
              "Pointer": false,
              "NodeType": "VarDeclNode",
              "Line": 15,
              "Children": [
                {
                  "Identifier": "gl_z",
                  "NodeType": "IdNode",
                  "Line": 15,
                  "Children": []
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```
</details>


#### Compact AST generated from Lua source:
```lua
function fact (n)
  if n == 0 then
    return 1
  else
    return n * fact(n-1)
  end
end
```

<details>
<summary>Click to expand!</summary>

```json
{"Name":null,"NodeType":"SourceNode","Line":1,"Children":[{"NodeType":"FuncDefNode","Line":1,"Children":[{"Modifiers":{"AccessModifiers":"Unspecified","QualifierFlags":"None"},"TypeName":"object","NodeType":"DeclSpecsNode","Line":1,"Children":[]},{"Pointer":false,"NodeType":"FuncDeclNode","Line":1,"Children":[{"Identifier":"fact","NodeType":"IdNode","Line":1,"Children":[]},{"IsVariadic":false,"NodeType":"FuncParamsNode","Line":1,"Children":[{"NodeType":"FuncParamNode","Line":1,"Children":[{"Modifiers":{"AccessModifiers":"Unspecified","QualifierFlags":"None"},"TypeName":"object","NodeType":"DeclSpecsNode","Line":1,"Children":[]},{"Pointer":false,"NodeType":"VarDeclNode","Line":1,"Children":[{"Identifier":"n","NodeType":"IdNode","Line":1,"Children":[]}]}]}]}]},{"NodeType":"BlockStatNode","Line":2,"Children":[{"NodeType":"IfStatNode","Line":2,"Children":[{"NodeType":"RelExprNode","Line":2,"Children":[{"Identifier":"n","NodeType":"IdNode","Line":2,"Children":[]},{"Symbol":"==","NodeType":"RelOpNode","Line":2,"Children":[]},{"Value":0,"Suffix":null,"TypeCode":"Int32","NodeType":"LitExprNode","Line":2,"Children":[]}]},{"NodeType":"BlockStatNode","Line":3,"Children":[{"Type":"Return","NodeType":"JumpStatNode","Line":3,"Children":[{"NodeType":"ExprListNode","Line":3,"Children":[{"Value":1,"Suffix":null,"TypeCode":"Int32","NodeType":"LitExprNode","Line":3,"Children":[]}]}]}]},{"NodeType":"BlockStatNode","Line":5,"Children":[{"Type":"Return","NodeType":"JumpStatNode","Line":5,"Children":[{"NodeType":"ExprListNode","Line":5,"Children":[{"NodeType":"ArithmExprNode","Line":5,"Children":[{"Identifier":"n","NodeType":"IdNode","Line":5,"Children":[]},{"Symbol":"*","NodeType":"ArithmOpNode","Line":5,"Children":[]},{"NodeType":"FuncCallExprNode","Line":5,"Children":[{"Identifier":"fact","NodeType":"IdNode","Line":5,"Children":[]},{"NodeType":"ExprListNode","Line":5,"Children":[{"NodeType":"ArithmExprNode","Line":5,"Children":[{"Identifier":"n","NodeType":"IdNode","Line":5,"Children":[]},{"Symbol":"-","NodeType":"ArithmOpNode","Line":5,"Children":[]},{"Value":1,"Suffix":null,"TypeCode":"Int32","NodeType":"LitExprNode","Line":5,"Children":[]}]}]}]}]}]}]}]}]}]}]}]}
```
</details>

## Visualizing ASTs:

Visualized AST using [LINVisualizer](https://github.com/LINVAST/LINVisualizer):

![tree](https://raw.githubusercontent.com/LINVAST/LINVisualizer/master/Samples/visualizer.png)


## Comparing ASTs using library extensions

Detailed examples comparing language-invariant ASTs can be seen [here](https://github.com/LINVAST/LINVAST.Imperative.Comparers).


### Sample comparing output for two C implementations of function swap

![swap](https://raw.githubusercontent.com/LINVAST/LINVAST.Imperative.Comparers/master/Samples/swap/valid_c-wrong_c.PNG)


# Extending AST library with new programming languages

Steps (for imperative language, in other cases only the namespace is different):
- Create (or use ![already made](https://github.com/antlr/grammars-v4/)) ANTLR4 grammar files
- Create lexer and parser for your grammar via ANTLR4 (using `-Dlanguage=CSharp` option)
- Create a builder namespace in `LINVAST.Imperative.Nodes.Builder.<YOUR_GRAMMAR>` as per examples already present for C or Lua
- Create a Builder type extending `ANTLR_GENERATED_BASE_VISITOR<ASTNode>` and implementing `IASTBuilder<ANTLR_GENERATED_PARSER_TYPE>` (change uppercase areas appropriately)
- Apply `[ASTBuilder(".YOUR_FILE_EXTENSION")]` attribute to the class in order for it to be automatically used when loading sources of that extension

Check out the Pseudocode PoC language ![grammar](LINVAST.Imperative/Builders/Pseudo/ANTLR/Pseudo.g4) and ![builder](LINVAST.Imperative/Builders/Pseudo/) as an example.
