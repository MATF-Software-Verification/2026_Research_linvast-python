using System.Linq;
using LINVAST.Imperative.Builders.Python;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Imperative.Builders.Python
{
    internal sealed class DeclarationTests
    {
        private readonly PythonASTBuilder builder = new();

        [Test]
        public void AnnotatedAssignmentBuildsTypedDeclaration()
        {
            var declaration = this.ParseSingle<DeclStatNode>("count: int = 1\n");
            var variable = declaration.DeclaratorList.Declarators.Single().As<VarDeclNode>();

            Assert.That(declaration.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(variable.Identifier, Is.EqualTo("count"));
            Assert.That(variable.Initializer, Is.TypeOf<LitExprNode>());
        }

        [Test]
        public void AnnotatedAssignmentPreservesGenericTypeText()
        {
            var declaration = this.ParseSingle<DeclStatNode>("items: List[int] = values\n");

            Assert.That(declaration.Specifiers.TypeName, Is.EqualTo("List[int]"));
        }

        [Test]
        public void AnnotatedAssignmentOnNonIdentifierTargetWithInitializerKeepsAssignment()
        {
            var stat = this.ParseSingle<ExprStatNode>("arr[0]: int = 42\n");

            Assert.That(stat.Expression, Is.TypeOf<AssignExprNode>());
            Assert.That(stat.Expression.As<AssignExprNode>().RightOperand.As<LitExprNode>().Value, Is.EqualTo(42));
        }

        [Test]
        public void DelSingleTarget()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del x\n");

            Assert.That(stat.Targets.Single(), Is.TypeOf<IdNode>());
            Assert.That(stat.Targets.Single().As<IdNode>().Identifier, Is.EqualTo("x"));
        }

        [Test]
        public void DelMultipleTargets()
        {
            var stat = this.ParseSingle<DeleteStatNode>("del a, b\n");

            Assert.That(stat.Targets.Select(t => t.As<IdNode>().Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void GlobalSingleIdentifier()
        {
            var stat = this.ParseSingle<GlobalStatNode>("global x\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "x" }));
        }

        [Test]
        public void GlobalMultipleIdentifiers()
        {
            var stat = this.ParseSingle<GlobalStatNode>("global x, y\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void NonlocalSingleIdentifier()
        {
            var stat = this.ParseSingle<NonlocalStatNode>("nonlocal a\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void NonlocalMultipleIdentifiers()
        {
            var stat = this.ParseSingle<NonlocalStatNode>("nonlocal a, b\n");

            Assert.That(stat.Identifiers.Select(i => i.Identifier), Is.EqualTo(new[] { "a", "b" }));
        }

        private TNode ParseSingle<TNode>(string source)
            where TNode : ASTNode
            => this.builder.BuildFromSource(source).As<SourceNode>().Children.Single().As<TNode>();

        [Test]
        public void TupleUnpackingSimple()
        {
            var source = "a, b = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children;

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();
            var varDecls = declStat.DeclaratorList.Declarators.ToList();

            Assert.That(varDecls.Count, Is.EqualTo(2));
            Assert.That(varDecls[0].As<VarDeclNode>().Identifier, Is.EqualTo("a"));
            Assert.That(varDecls[0].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));
            Assert.That(varDecls[1].As<VarDeclNode>().Identifier, Is.EqualTo("b"));
            Assert.That(varDecls[1].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(2));
        }
        [Test]
        public void TupleUnpackingSimpleLong()
        {
            var source = "a, b, c, d, e = 1, 2, 3, 4, 5\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children;

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();
            var varDecls = declStat.DeclaratorList.Declarators.ToList();

            Assert.That(varDecls.Count, Is.EqualTo(5));
        }

        [Test]
        public void TupleUnpackingMismatchedCounts()
        {
            var source = "x, y, z = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children;

            Assert.That(nodes.Single(), Is.TypeOf<ExprStatNode>());
            var assign = nodes.Single().As<ExprStatNode>().Expression.As<AssignExprNode>();
            Assert.That(assign.LeftOperand.As<ArrInitExprNode>().Expressions.Count(), Is.EqualTo(3));
            Assert.That(assign.RightOperand.As<ArrInitExprNode>().Expressions.Count(), Is.EqualTo(2));
        }

        [Test]
        public void TupleUnpackingWithPreviouslyDeclaredIdentifierKeepsAssignment()
        {
            var source = "a: int = 0\na, b = 1, 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Count, Is.EqualTo(2));

            var decl = nodes[0].As<DeclStatNode>();
            Assert.That(decl.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(decl.DeclaratorList.Declarators.Single().As<VarDeclNode>().Identifier, Is.EqualTo("a"));
            Assert.That(decl.DeclaratorList.Declarators.Single().As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(0));

            var assign = nodes[1].As<ExprStatNode>().Expression.As<AssignExprNode>();
            Assert.That(assign.LeftOperand.As<ArrInitExprNode>().Expressions.Select(e => e.As<IdNode>().Identifier),
                Is.EqualTo(new[] { "a", "b" }));
            Assert.That(assign.RightOperand.As<ArrInitExprNode>().Expressions.Select(e => e.As<LitExprNode>().Value),
                Is.EqualTo(new object[] { 1, 2 }));
        }

        [Test]
        public void ReassignmentAfterAnnotatedDeclarationIsNotRedeclared()
        {
            var source = "count: int = 1\ncount = 2\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Count, Is.EqualTo(2));

            var decl = nodes[0].As<DeclStatNode>();
            Assert.That(decl.Specifiers.TypeName, Is.EqualTo("int"));
            Assert.That(decl.DeclaratorList.Declarators.Single().As<VarDeclNode>().Identifier, Is.EqualTo("count"));
            Assert.That(decl.DeclaratorList.Declarators.Single().As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));

            var assign = nodes[1].As<ExprStatNode>().Expression.As<AssignExprNode>();
            Assert.That(assign.LeftOperand.As<IdNode>().Identifier, Is.EqualTo("count"));
            Assert.That(assign.RightOperand.As<LitExprNode>().Value, Is.EqualTo(2));
        }

        [Test]
        public void TupleUnpackingAnnotatedTypeIsPromoted()
        {
            var source = "(a, b): tuple = (1, 2)\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();

            Assert.That(declStat.Specifiers.TypeName, Is.EqualTo("tuple"));

            var varDecls = declStat.DeclaratorList.Declarators.ToList();
            Assert.That(varDecls.Count, Is.EqualTo(2));
            Assert.That(varDecls[0].As<VarDeclNode>().Identifier, Is.EqualTo("a"));
            Assert.That(varDecls[0].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));
            Assert.That(varDecls[1].As<VarDeclNode>().Identifier, Is.EqualTo("b"));
            Assert.That(varDecls[1].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(2));
        }

        [Test]
        public void TupleUnpackingInList()
        {
            var forStat = this.ParseSingle<ForStatNode>("for a, b in pairs:\n    pass\n");

            Assert.That(forStat.ForDeclaration, Is.TypeOf<DeclListNode>());

            Assert.That(
                forStat.ForDeclaration!.As<DeclListNode>().Declarators.Select(decl => decl.As<VarDeclNode>().Identifier),
                Is.EqualTo(new[] { "a", "b" }));

            Assert.That(forStat.Condition, Is.TypeOf<IdNode>());
            Assert.That(forStat.Condition.As<IdNode>().Identifier, Is.EqualTo("pairs"));

            Assert.That(forStat.Statement.As<BlockStatNode>().Children.Single(), Is.TypeOf<EmptyStatNode>());
        }

        [Test]
        public void TupleUnpackingStarSimpleEnd()
        {
            var source = "a, *b = 1, 2, 3\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();

            var varDecls = declStat.DeclaratorList.Declarators.ToList();
            Assert.That(varDecls.Count, Is.EqualTo(2));
            Assert.That(varDecls[0].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));
            this.AssertInitializerArrayValues(varDecls[1].As<VarDeclNode>(), new object[] { 2, 3 });
        }

        [Test]
        public void TupleUnpackingStarSimpleStart()
        {
            var source = "*a, b = 1, 2, 3\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();

            var varDecls = declStat.DeclaratorList.Declarators.ToList();
            Assert.That(varDecls.Count, Is.EqualTo(2));
            this.AssertInitializerArrayValues(varDecls[0].As<VarDeclNode>(), new object[] { 1, 2 });
            Assert.That(varDecls[1].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(3));
        }

        [Test]
        public void TupleUnpackingStarSimpleMiddle()
        {
            var source = "a, *b, c = 1, 2, 3, 4\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();

            var varDecls = declStat.DeclaratorList.Declarators.ToList();
            Assert.That(varDecls.Count, Is.EqualTo(3));
            Assert.That(varDecls[0].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));
            this.AssertInitializerArrayValues(varDecls[1].As<VarDeclNode>(), new object[] { 2, 3 });
            Assert.That(varDecls[2].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(4));
        }

        [Test]
        public void TupleUnpackingStarWithType()
        {
            var source = "(a, *b): tuple = (1, 2, 3)\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Single(), Is.TypeOf<DeclStatNode>());
            var declStat = nodes.Single().As<DeclStatNode>();
            Assert.That(declStat.Specifiers.TypeName, Is.EqualTo("tuple"));

            var varDecls = declStat.DeclaratorList.Declarators.ToList();
            Assert.That(varDecls.Count, Is.EqualTo(2));
            Assert.That(varDecls[0].As<VarDeclNode>().Initializer!.As<LitExprNode>().Value, Is.EqualTo(1));
            this.AssertInitializerArrayValues(varDecls[1].As<VarDeclNode>(), new object[] { 2, 3 });
        }

        [Test]
        public void TupleUnpackingStarWithTypeAndPreviousDeclarationPromotesToDeclaration()
        {
            var source = "a: int = 0\n(a, *b): tuple = (1, 2, 3)\n";
            var nodes = this.builder.BuildFromSource(source).As<SourceNode>().Children.ToList();

            Assert.That(nodes.Count, Is.EqualTo(2));
            Assert.That(nodes[0], Is.TypeOf<DeclStatNode>());

            var decl = nodes[1].As<DeclStatNode>();
            Assert.That(decl.Specifiers.TypeName, Is.EqualTo("tuple"));
        }

        private void AssertInitializerArrayValues(VarDeclNode declaration, object[] expectedValues)
        {
            var initializer = declaration.Initializer!.As<ArrInitExprNode>();
            Assert.That(initializer.Expressions.Select(e => e.As<LitExprNode>().Value), Is.EqualTo(expectedValues));
        }
    }
}
