using System.Linq;
using LINVAST.Imperative.Builders.C;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Imperative.Visitors;
using LINVAST.Nodes;
using LINVAST.Tests.Imperative.Builders.Common;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.C
{
    internal sealed class DeclarationTests : DeclarationTestsBase
    {
        [Test]
        public void SimpleDeclarationTest()
        {
            this.AssertVariableDeclaration("int x;", "x", "int");
            this.AssertVariableDeclaration("float y;", "y", "float");
            this.AssertVariableDeclaration("Point z;", "z", "Point");
            this.AssertVariableDeclaration("time_t y_y;", "y_y", "time_t");
        }

        [Test]
        public void DeclarationSpecifierTest()
        {
            this.AssertVariableDeclaration(
                "static volatile  time_t x;",
                "x",
                "time_t",
                AccessModifiers.Unspecified,
                QualifierFlags.Static | QualifierFlags.Volatile
            );
            this.AssertVariableDeclaration(
                "static  extern  const    unsigned  int x;",
                "x",
                "unsigned int",
                AccessModifiers.Public,
                QualifierFlags.Static | QualifierFlags.Const
            );
        }

        [Test]
        public void InitializerDeclarationTest()
        {
            this.AssertVariableDeclaration("static signed int x = 5;", "x", "int", AccessModifiers.Unspecified, QualifierFlags.Static, 5);
        }

        [Test]
        public void InitializerExpressionDeclarationTest()
        {
            this.AssertVariableDeclaration("unsigned short x = 1 << 2 * 4;", "x", "unsigned short", AccessModifiers.Unspecified, value: 1 << 8);
        }

        [Test]
        public void FunctionDeclarationNoParamsTest()
        {
            this.AssertFunctionDeclaration("void f();", "f", "void");
            this.AssertFunctionDeclaration("extern static void f();", "f", "void", AccessModifiers.Public, QualifierFlags.Static);
        }

        [Test]
        public void FunctionDeclarationVariadicTest()
        {
            this.AssertFunctionDeclaration(
                "void f(const int x, ...);",
                "f",
                "void",
                isVariadic: true,
                @params: (QualifierFlags.Const, "int", "x")
            );
        }

        [Test]
        public void FunctionDeclarationQualifiersTest()
        {
            this.AssertFunctionDeclaration(
                "extern static time_t f(int x, const volatile unsigned long long y, ...);",
                "f",
                "time_t",
                AccessModifiers.Public,
                QualifierFlags.Static,
                true,
                (QualifierFlags.None, "int", "x"),
                (QualifierFlags.Const | QualifierFlags.Volatile, "unsigned long long", "y")
            );
        }

        [Test]
        public void FunctionDeclarationArrayParamsTest()
        {
            this.AssertFunctionDeclaration(
                "int f(int a[], const float b[4]);",
                "f",
                "int",
                AccessModifiers.Unspecified,
                QualifierFlags.None,
                false,
                (QualifierFlags.None, "int", "a"),
                (QualifierFlags.Const, "float", "b")
            );
        }

        [Test]
        public void SimpleDeclarationListTest()
        {
            this.AssertVariableDeclarationList(
                "static unsigned int x, y, z;",
                "unsigned int",
                AccessModifiers.Unspecified, QualifierFlags.Static,
                ("x", null), ("y", null), ("z", null)
            );
        }

        [Test]
        public void IntDeclarationListWithInitializersTest()
        {
            this.AssertVariableDeclarationList(
                "extern static int x, y = 7 + (4 - 3), z = 3, w = 3*4 + 7*5, t = 2 >> (3 << 4);",
                "int",
                AccessModifiers.Public, QualifierFlags.Static,
                ("x", null), ("y", 7 + (4 - 3)), ("z", 3), ("w", 47), ("t", 2 >> (3 << 4))
            );

            this.AssertVariableDeclarationList(
                "extern static int x = 010, y = 0xfF, z = 0xabcdef, w = 0xA + 010, t = 0x1 << 4;",
                "int",
                AccessModifiers.Public, QualifierFlags.Static,
                ("x", 8), ("y", 0xFF), ("z", 0xABCDEF), ("w", 0xA + 8), ("t", 1 << 4)
            );

            this.AssertVariableDeclarationList(
                "extern static int x = 010u, y = 0xfFl, z = 0xabcdefUL, w = 0xAL + 010l, t = 0x1uLL << 4;",
                "int",
                AccessModifiers.Public, QualifierFlags.Static,
                ("x", 8), ("y", 0xFF), ("z", 0xABCDEF), ("w", 0xA + 8), ("t", 1 << 4)
            );
        }

        [Test]
        public void FloatDeclarationListWithInitializersTest()
        {
            this.AssertVariableDeclarationList(
                "float x, y = 7.1 + 4.2, z = 3.0, w = 3.2*4.45 + 7.2*5.11 - (5.0/2.5);",
                "float",
                AccessModifiers.Unspecified, QualifierFlags.None,
                ("x", null), ("y", 11.3), ("z", 3.0), ("w", 49.032)
            );

            this.AssertVariableDeclarationList(
                "float x, y = 7.1f + 4.2, z = 3.0f, w = 3.2L*4.45f + 7.2f*5.11f - (5.0/2.5);",
                "float",
                AccessModifiers.Unspecified, QualifierFlags.None,
                ("x", null), ("y", 11.3), ("z", 3.0f), ("w", 49.032)
            );

            this.AssertVariableDeclarationList(
                "float x = -1e-10, y = 7.1f + 4.2f, z = +3e-10, w = 3.2e+0*4.45e0 + 7.2*5.11 - (5.0/2.5);",
                "float",
                AccessModifiers.Unspecified, QualifierFlags.None,
                ("x", -1e-10), ("y", 11.3f), ("z", 3e-10), ("w", 49.032)
            );
        }

        [Test]
        public void BoolDeclarationListWithInitializersTest()
        {
            this.AssertVariableDeclarationList(
                "bool x, y = 1 == 1, z = 3 <= 4, w = 4 != (3 + 1);",
                "bool",
                AccessModifiers.Unspecified, QualifierFlags.None,
                ("x", null), ("y", true), ("z", true), ("w", false)
            );
        }

        [Test]
        public void StringDeclarationListWithInitializersTest()
        {
            this.AssertVariableDeclarationList(
                @"char* w1, w2 = ""abc"", w3 = ""aa"" + ""bb"";",
                "char",
                AccessModifiers.Unspecified, QualifierFlags.None,
                ("w1", null), ("w2", "abc"), ("w3", "aabb")
            );
        }

        [Test]
        public void ArrayDeclarationTest()
        {
            this.AssertArrayDeclaration(
                "static unsigned int x[3];",
                "unsigned int",
                "x",
                3,
                AccessModifiers.Unspecified, QualifierFlags.Static
            );
            this.AssertArrayDeclaration(
                "static unsigned int x[3 + 4 * 1 - 3];",
                "unsigned int",
                "x",
                4,
                AccessModifiers.Unspecified, QualifierFlags.Static
            );
            this.AssertArrayDeclaration(
                "const unsigned long long int x[(2 << 1) / 2];",
                "unsigned long long int",
                "x",
                2,
                AccessModifiers.Unspecified, QualifierFlags.Const
            );

            ArrDeclNode matrix = this.GenerateAST("int matrix[2][3];")
                .As<DeclStatNode>()
                .DeclaratorList.Declarators.Single()
                .As<ArrDeclNode>();
            Assert.That(matrix.SizeExpression, Is.InstanceOf<ExprListNode>());
            Assert.That(matrix.SizeExpression!.As<ExprListNode>().Expressions.Select(ConstantExpressionEvaluator.Evaluate),
                Is.EqualTo(new object[] { 2, 3 }));

            ArrDeclNode cube = this.GenerateAST("int cube[2][3][4];")
                .As<DeclStatNode>()
                .DeclaratorList.Declarators.Single()
                .As<ArrDeclNode>();
            Assert.That(cube.SizeExpression, Is.InstanceOf<ExprListNode>());
            Assert.That(cube.SizeExpression!.As<ExprListNode>().Expressions.Select(ConstantExpressionEvaluator.Evaluate),
                Is.EqualTo(new object[] { 2, 3, 4 }));

            this.AssertArrayDeclaration("int values[static 4];", "int", "values", 4);
        }

        [Test]
        public void FunctionPointerDeclarationTest()
        {
            DeclStatNode decl = this.GenerateAST("int (*handler)(int x);").As<DeclStatNode>();
            FuncDeclNode func = decl.DeclaratorList.Declarators.Single().As<FuncDeclNode>();

            Assert.That(func.Identifier, Is.EqualTo("handler"));
            Assert.That(func.PointerLevel, Is.EqualTo(1));
            Assert.That(func.Parameters, Has.Exactly(1).Items);

            DeclStatNode compareDecl = this.GenerateAST("int (*compare)(const char *left, const char *right);").As<DeclStatNode>();
            FuncDeclNode compare = compareDecl.DeclaratorList.Declarators.Single().As<FuncDeclNode>();
            Assert.That(compare.Identifier, Is.EqualTo("compare"));
            Assert.That(compare.PointerLevel, Is.EqualTo(1));
            Assert.That(compare.Parameters, Has.Exactly(2).Items);
            Assert.That(compare.Parameters!.Select(p => p.Specifiers.TypeName), Is.EqualTo(new[] { "char*", "char*" }));
            Assert.That(compare.Parameters!.Select(p => p.Declarator.Identifier), Is.EqualTo(new[] { "left", "right" }));
        }

        [Test]
        public void ArrayDeclarationInitializerTest()
        {
            this.AssertArrayDeclaration(
                "extern int x[3] = { 3, 4, 5 };",
                "int",
                "x",
                3,
                AccessModifiers.Public, QualifierFlags.None,
                3, 4, 5
            );
            this.AssertArrayDeclaration(
                "const int x[3] = { 1 + 2, 2 << 1 };",
                "int",
                "x",
                3,
                AccessModifiers.Unspecified, QualifierFlags.Const,
                3, 4
            );
            this.AssertArrayDeclaration(
                "int x[] = { 1 + 2, 2 << 1 };",
                "int",
                "x",
                null,
                AccessModifiers.Unspecified, QualifierFlags.None,
                3, 4
            );
            this.AssertArrayDeclaration(
                "volatile int x[] = { 1 + 2, 2 << 1, 200, 0x31 };",
                "int",
                "x",
                null,
                AccessModifiers.Unspecified, QualifierFlags.Volatile,
                3, 4, 200, 0x31
            );
        }

        [Test]
        public void EmptyAndStaticAssertDeclarationTest()
        {
            DeclStatNode emptyDeclaration = this.GenerateAST("int;").As<DeclStatNode>();
            Assert.That(emptyDeclaration.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(emptyDeclaration.DeclaratorList.Declarators, Is.Empty);

            Assert.That(this.GenerateAST("_Static_assert(1, \"ok\");"), Is.InstanceOf<EmptyStatNode>());
        }

        [Test]
        public void DesignatedArrayInitializerTest()
        {
            ArrDeclNode array = this.GenerateAST("int x[] = { [2] = 3, 4 };")
                .As<DeclStatNode>()
                .DeclaratorList.Declarators.Single()
                .As<ArrDeclNode>();

            Assert.That(array.Initializer!.Initializers.Select(ConstantExpressionEvaluator.Evaluate), Is.EqualTo(new object[] { 3, 4 }));
        }

        [Test]
        public void StructDefinitionTest()
        {
            StructNode ast = this.GenerateAST("struct Point { int x; float y; };").As<StructNode>();
            TypeDeclNode decl = ast.DeclaratorList.Declarators.Single().As<TypeDeclNode>();

            Assert.That(ast.Specifiers.TypeName, Is.EqualTo("Point"));
            Assert.That(decl.Identifier, Is.EqualTo("Point"));
            Assert.That(decl.Declarations.Select(d => d.Specifiers.TypeName), Is.EqualTo(new[] { "int", "float" }));
            Assert.That(decl.Declarations.Select(d => d.DeclaratorList.Declarators.Single().Identifier), Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void StructVariableDeclarationTest()
        {
            this.AssertVariableDeclaration("struct Point p;", "p", "struct Point");

            DeclStatNode pointerDecl = this.GenerateAST("struct Point *p;").As<DeclStatNode>();
            VarDeclNode pointer = pointerDecl.DeclaratorList.Declarators.Single().As<VarDeclNode>();
            Assert.That(pointerDecl.Specifiers.TypeName, Is.EqualTo("struct Point"));
            Assert.That(pointer.Identifier, Is.EqualTo("p"));
            Assert.That(pointer.PointerLevel, Is.EqualTo(1));

            this.AssertVariableDeclaration("struct Point { int x; } p;", "p", "struct Point");
            this.AssertVariableDeclaration("struct { int x; } p;", "p", "struct <anonymous>");
        }

        [Test]
        public void StructFieldDeclarationTest()
        {
            StructNode ast = this.GenerateAST("struct Packet { unsigned flags:3; int values[3]; struct Point *next; };").As<StructNode>();
            TypeDeclNode decl = ast.DeclaratorList.Declarators.Single().As<TypeDeclNode>();
            DeclStatNode[] fields = decl.Declarations.ToArray();

            Assert.That(fields, Has.Exactly(3).Items);
            Assert.That(fields[0].Specifiers.TypeName, Is.EqualTo("unsigned"));
            Assert.That(fields[0].DeclaratorList.Declarators.Single().Identifier, Is.EqualTo("flags"));

            ArrDeclNode values = fields[1].DeclaratorList.Declarators.Single().As<ArrDeclNode>();
            Assert.That(fields[1].Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(values.Identifier, Is.EqualTo("values"));
            Assert.That(ConstantExpressionEvaluator.Evaluate(values.SizeExpression!), Is.EqualTo(3));

            VarDeclNode next = fields[2].DeclaratorList.Declarators.Single().As<VarDeclNode>();
            Assert.That(fields[2].Specifiers.TypeName, Is.EqualTo("struct Point"));
            Assert.That(next.Identifier, Is.EqualTo("next"));
            Assert.That(next.PointerLevel, Is.EqualTo(1));
        }

        [Test]
        public void StructFieldDeclarationListTest()
        {
            StructNode ast = this.GenerateAST("struct Point { int x, y; };").As<StructNode>();
            DeclStatNode field = ast.DeclaratorList.Declarators.Single().As<TypeDeclNode>().Declarations.Single();

            Assert.That(field.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(field.DeclaratorList.Declarators.Select(d => d.Identifier), Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void StructBitFieldDeclarationListTest()
        {
            StructNode ast = this.GenerateAST("struct Flags { unsigned a:1, b:2; };").As<StructNode>();
            DeclStatNode field = ast.DeclaratorList.Declarators.Single().As<TypeDeclNode>().Declarations.Single();

            Assert.That(field.Specifiers.TypeName, Is.EqualTo("unsigned"));
            Assert.That(field.DeclaratorList.Declarators.Select(d => d.Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void StructUnnamedBitFieldAndStaticAssertTest()
        {
            StructNode ast = this.GenerateAST("struct Flags { _Static_assert(1, \"ok\"); unsigned :1; unsigned a:1; };").As<StructNode>();
            TypeDeclNode decl = ast.DeclaratorList.Declarators.Single().As<TypeDeclNode>();

            Assert.That(decl.Declarations, Has.Exactly(2).Items);
            Assert.That(decl.Declarations.First().DeclaratorList.Declarators, Is.Empty);
            Assert.That(decl.Declarations.Last().DeclaratorList.Declarators.Single().Identifier, Is.EqualTo("a"));
        }


        protected override ASTNode GenerateAST(string src)
            => new CASTBuilder().BuildFromSource(src, p => p.externalDeclaration());
    }
}
