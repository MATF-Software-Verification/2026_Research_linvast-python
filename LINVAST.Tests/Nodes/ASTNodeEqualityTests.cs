using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;
using NUnit.Framework;

namespace LINVAST.Tests.Nodes
{
    internal sealed class ASTNodeEqualityTests
    {
        [Test]
        public void BasicEqualityTest()
        {
            ASTNode ast1 = new SourceNode(new BlockStatNode(1));
            ASTNode ast2 = new SourceNode(new BlockStatNode(100));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void BasicDifferenceTest()
        {
            ASTNode ast1 = new SourceNode(new BlockStatNode(1));
            ASTNode ast2 = new SourceNode(new VarDeclNode(10, new IdNode(10, "x")));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void ExtraNodesDifferenceTest()
        {
            ASTNode ast1 = new SourceNode(new BlockStatNode(1));
            ASTNode ast2 = new SourceNode(new BlockStatNode(1), new BlockStatNode(2));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void BasicChildrenEqualityTest()
        {
            ASTNode ast1 = new SourceNode(new VarDeclNode(10, new IdNode(10, "x")));
            ASTNode ast2 = new SourceNode(new VarDeclNode(12, new IdNode(12, "x")));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void BasicChildrenDifferenceTest()
        {
            ASTNode ast1 = new SourceNode(new VarDeclNode(10, new IdNode(10, "x")));
            ASTNode ast2 = new SourceNode(new VarDeclNode(10, new IdNode(10, "y")));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void VariableDeclarationEqualityTest()
        {
            ASTNode ast1 = new DeclStatNode(
                1,
                new DeclSpecsNode(1, "public static", " int"),
                new DeclListNode(
                    1,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            ASTNode ast2 = new DeclStatNode(
                2,
                new DeclSpecsNode(2, "public static", "int"),
                new DeclListNode(
                    2,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void VariableDeclarationDifferenceTest1()
        {
            ASTNode ast1 = new DeclStatNode(
                1,
                new DeclSpecsNode(1, "public static", "int"),
                new DeclListNode(
                    1,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 2),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            ASTNode ast2 = new DeclStatNode(
                2,
                new DeclSpecsNode(2, "public static", "int"),
                new DeclListNode(
                    2,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void VariableDeclarationDifferenceTest2()
        {
            ASTNode ast1 = new DeclStatNode(
                1,
                new DeclSpecsNode(1, "public static", "int"),
                new DeclListNode(
                    1,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "-"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            ASTNode ast2 = new DeclStatNode(
                2,
                new DeclSpecsNode(2, "public static", "int"),
                new DeclListNode(
                    2,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void VariableDeclarationDifferenceTest3()
        {
            ASTNode ast1 = new DeclStatNode(
                1,
                new DeclSpecsNode(1, "static", "int"),
                new DeclListNode(
                    1,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            ASTNode ast2 = new DeclStatNode(
                2,
                new DeclSpecsNode(2, "public static", "int"),
                new DeclListNode(
                    2,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void VariableDeclarationDifferenceTest4()
        {
            ASTNode ast1 = new DeclStatNode(
                1,
                new DeclSpecsNode(1, "public static", "float"),
                new DeclListNode(
                    1,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            ASTNode ast2 = new DeclStatNode(
                2,
                new DeclSpecsNode(2, "public static", "int"),
                new DeclListNode(
                    2,
                    new VarDeclNode(
                        2,
                        new IdNode(2, "x"),
                        new ArithmExprNode(
                            3,
                            new LitExprNode(4, 1),
                            ArithmOpNode.FromSymbol(4, "+"),
                            new LitExprNode(4, 1)
                        )
                    )
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void FunctionDefenitionEqualityTest()
        {
            ASTNode ast1 = new FuncNode(
                1,
                new DeclSpecsNode(2, "public static", "void"),
                new FuncDeclNode(
                    2,
                    new IdNode(2, "f"),
                    new FuncParamsNode(
                        2,
                        new FuncParamNode(
                            3,
                            new DeclSpecsNode(3, "const", "time_t"),
                            new ArrDeclNode(3, new IdNode(3, "arr"))
                        )
                    ),
                    new BlockStatNode(3, new JumpStatNode(4, new LitExprNode(4, "2", "u")))
                )
            );
            ASTNode ast2 = new FuncNode(
                1,
                new DeclSpecsNode(3, "public static", "void"),
                new FuncDeclNode(
                    3,
                    new IdNode(3, "f"),
                    new FuncParamsNode(
                        4,
                        new FuncParamNode(
                            4,
                            new DeclSpecsNode(5, "const", "time_t"),
                            new ArrDeclNode(6, new IdNode(6, "arr"))
                        )
                    ),
                    new BlockStatNode(7, new JumpStatNode(8, new LitExprNode(8, "2", "u")))
                )
            );
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void FunctionDefenitionDifferenceTest1()
        {
            ASTNode ast1 = new FuncNode(
                1,
                new DeclSpecsNode(2, "public static", "void"),
                new FuncDeclNode(
                    2,
                    new IdNode(2, "f"),
                    new FuncParamsNode(
                        2,
                        new FuncParamNode(
                            3,
                            new DeclSpecsNode(3, "", "time_t"),
                            new ArrDeclNode(3, new IdNode(3, "arr"))
                        )
                    ),
                    new BlockStatNode(3, new JumpStatNode(4, new LitExprNode(4, "2", "u")))
                )
            );
            ASTNode ast2 = new FuncNode(
                1,
                new DeclSpecsNode(3, "public static", "void"),
                new FuncDeclNode(
                    3,
                    new IdNode(4, "f"),
                    new FuncParamsNode(
                        4,
                        new FuncParamNode(
                            4,
                            new DeclSpecsNode(5, "const", "time_t"),
                            new ArrDeclNode(6, new IdNode(6, "arr"))
                        )
                    ),
                    new BlockStatNode(7, new JumpStatNode(8, new LitExprNode(8, "2", "u")))
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void FunctionDefenitionDifferenceTest2()
        {
            ASTNode ast1 = new FuncNode(
                1,
                new DeclSpecsNode(2, "public static", "void"),
                new FuncDeclNode(
                    2,
                    new IdNode(2, "f"),
                    new FuncParamsNode(
                        2,
                        new FuncParamNode(
                            3,
                            new DeclSpecsNode(3, "const", "time_t"),
                            new VarDeclNode(3, new IdNode(3, "arr"))
                        )
                    ),
                    new BlockStatNode(3, new JumpStatNode(4, new LitExprNode(4, "2", "u")))
                )
            );
            ASTNode ast2 = new FuncNode(
                1,
                new DeclSpecsNode(3, "public static", "void"),
                new FuncDeclNode(
                    3,
                    new IdNode(4, "f"),
                    new FuncParamsNode(
                        4,
                        new FuncParamNode(
                            4,
                            new DeclSpecsNode(5, "const", "time_t"),
                            new ArrDeclNode(6, new IdNode(6, "arr"))
                        )
                    ),
                    new BlockStatNode(7, new JumpStatNode(8, new LitExprNode(8, "2", "u")))
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void FunctionDefenitionDifferenceTest3()
        {
            ASTNode ast1 = new FuncNode(
                1,
                new DeclSpecsNode(2, "public static", "void"),
                new FuncDeclNode(
                    2,
                    new IdNode(2, "f"),
                    new FuncParamsNode(
                        2,
                        new FuncParamNode(
                            3,
                            new DeclSpecsNode(3, "const", "time_t"),
                            new ArrDeclNode(3, new IdNode(3, "arr"))
                        )
                    ),
                    new BlockStatNode(3, new JumpStatNode(4, new LitExprNode(4, "2", "u")))
                )
            );
            ASTNode ast2 = new FuncNode(
                1,
                new DeclSpecsNode(3, "public static", "int"),
                new FuncDeclNode(
                    3,
                    new IdNode(4, "f"),
                    new FuncParamsNode(
                        4,
                        new FuncParamNode(
                            4,
                            new DeclSpecsNode(5, "const", "time_t"),
                            new ArrDeclNode(6, new IdNode(6, "arr"))
                        )
                    ),
                    new BlockStatNode(7, new JumpStatNode(8, new LitExprNode(8, "2", "l")))
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void FunctionDefenitionDifferenceTest4()
        {
            ASTNode ast1 = new FuncNode(
                1,
                new DeclSpecsNode(2, "public static", "void"),
                new FuncDeclNode(
                    2,
                    new IdNode(2, "f"),
                    new FuncParamsNode(
                        2,
                        new FuncParamNode(
                            3,
                            new DeclSpecsNode(3, "", "time_t"),
                            new ArrDeclNode(3, new IdNode(3, "arr"))
                        )
                    ),
                    new BlockStatNode(3, new JumpStatNode(4, new LitExprNode(4, "2", "u")))
                )
            );
            ASTNode ast2 = new FuncNode(
                1,
                new DeclSpecsNode(3, "public static", "void"),
                new FuncDeclNode(
                    3,
                    new IdNode(4, "f"),
                    new FuncParamsNode(
                        4,
                        new FuncParamNode(
                            4,
                            new DeclSpecsNode(5, "const", "time_t"),
                            new ArrDeclNode(6, new IdNode(6, "arr"))
                        )
                    ),
                    new BlockStatNode(7, new JumpStatNode(8, new LitExprNode(8, "2", "u")), new EmptyStatNode(10))
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void ExpressionEqualityTest()
        {
            ASTNode ast1 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    2,
                    new LitExprNode(2, 3),
                    ArithmOpNode.FromSymbol(2, "+"),
                    new LitExprNode(2, 3)
                ),
                ArithmOpNode.FromSymbol(2, "-"),
                new ArithmExprNode(
                    2,
                    new LitExprNode(2, 3),
                    ArithmOpNode.FromSymbol(2, "+"),
                    new LitExprNode(2, 3)
                )
            );
            ASTNode ast2 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                ),
                ArithmOpNode.FromSymbol(1, "-"),
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                )
            );
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void ExpressionDifferenceTest1()
        {
            ASTNode ast1 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    2,
                    new LitExprNode(2, 3),
                    ArithmOpNode.FromSymbol(2, "+"),
                    new LitExprNode(2, 3)
                ),
                ArithmOpNode.FromSymbol(2, "-"),
                new LitExprNode(2, 6)
            );
            ASTNode ast2 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                ),
                ArithmOpNode.FromSymbol(1, "-"),
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void ExpressionDifferenceTest2()
        {
            ASTNode ast1 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    2,
                    new LitExprNode(2, 3),
                    ArithmOpNode.FromSymbol(2, "+"),
                    new LitExprNode(2, 3)
                ),
                ArithmOpNode.FromSymbol(2, "-"),
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "*"),
                    new LitExprNode(1, 3)
                )
            );
            ASTNode ast2 = new ArithmExprNode(
                1,
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                ),
                ArithmOpNode.FromSymbol(1, "-"),
                new ArithmExprNode(
                    1,
                    new LitExprNode(1, 3),
                    ArithmOpNode.FromSymbol(1, "+"),
                    new LitExprNode(1, 3)
                )
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void BranchingStatementEqualityTest1()
        {
            ASTNode ast1 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1)
            );
            ASTNode ast2 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1)
            );
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void BranchingStatementEqualityTest2()
        {
            ASTNode ast1 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1),
                new BlockStatNode(1, new EmptyStatNode(1))
            );
            ASTNode ast2 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1),
                new BlockStatNode(1, new EmptyStatNode(1))
            );
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void BranchingStatementDifferenceTest1()
        {
            ASTNode ast1 = new IfStatNode(
                1,
                new LitExprNode(1, false),
                new EmptyStatNode(1),
                new BlockStatNode(1, new EmptyStatNode(1))
            );
            ASTNode ast2 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1),
                new BlockStatNode(1, new EmptyStatNode(1))
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void BranchingStatementDifferenceTest2()
        {
            ASTNode ast1 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1),
                new BlockStatNode(1, new EmptyStatNode(1))
            );
            ASTNode ast2 = new IfStatNode(
                1,
                new LitExprNode(1, true),
                new EmptyStatNode(1)
            );
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void DeleteStatementEqualityTest1()
        {
            ASTNode ast1 = new DeleteStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new DeleteStatNode(5, new IdNode(5, "x"), new IdNode(5, "y"));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void DeleteStatementDifferenceTest2()
        {
            ASTNode ast1 = new DeleteStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new DeleteStatNode(5, new IdNode(5, "x"), new IdNode(5, "z"));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void GlobalStatementEqualityTest1()
        {
            ASTNode ast1 = new GlobalStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new GlobalStatNode(5, new IdNode(5, "x"), new IdNode(5, "y"));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void GlobalStatementDifferenceTest2()
        {
            ASTNode ast1 = new GlobalStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new GlobalStatNode(5, new IdNode(5, "x"), new IdNode(5, "z"));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void GlobalStatementDifferenceTest3()
        {
            ASTNode ast1 = new GlobalStatNode(1, new IdNode(1, "x"));
            ASTNode ast2 = new GlobalStatNode(5, new IdNode(5, "x"), new IdNode(5, "y"));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void NonlocalStatementEqualityTest1()
        {
            ASTNode ast1 = new NonlocalStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new NonlocalStatNode(5, new IdNode(5, "x"), new IdNode(5, "y"));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void NonlocalStatementDifferenceTest2()
        {
            ASTNode ast1 = new NonlocalStatNode(1, new IdNode(1, "x"), new IdNode(1, "y"));
            ASTNode ast2 = new NonlocalStatNode(5, new IdNode(5, "x"), new IdNode(5, "z"));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void NonlocalStatementDifferenceTest3()
        {
            ASTNode ast1 = new NonlocalStatNode(1, new IdNode(1, "x"));
            ASTNode ast2 = new NonlocalStatNode(5, new IdNode(5, "x"), new IdNode(5, "y"));
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void YieldExpressionEqualityTest()
        {
            ASTNode ast1 = new YieldExprNode(1, new LitExprNode(1, 1L));
            ASTNode ast2 = new YieldExprNode(2, new LitExprNode(2, 1L));
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void YieldExpressionDelegationDifferenceTest()
        {
            ASTNode ast1 = new YieldExprNode(1, new LitExprNode(1, 1L), isDelegating: true);
            ASTNode ast2 = new YieldExprNode(1, new LitExprNode(1, 1L), isDelegating: false);
            AssertNodes(ast1, ast2, eq: false);
        }

        [Test]
        public void SequencePatternEqualityTest()
        {
            ASTNode ast1 = new PatternSequenceNode(1, SequencePatternKind.Bracket, new PatternNode[] { new PatternCaptureNode(1, new IdNode(1, "x")) });
            ASTNode ast2 = new PatternSequenceNode(2, SequencePatternKind.Bracket, new PatternNode[] { new PatternCaptureNode(2, new IdNode(2, "x")) });
            AssertNodes(ast1, ast2, eq: true);
        }

        [Test]
        public void SequencePatternKindDifferenceTest()
        {
            ASTNode ast1 = new PatternSequenceNode(1, SequencePatternKind.Bracket, new PatternNode[] { new PatternCaptureNode(1, new IdNode(1, "x")) });
            ASTNode ast2 = new PatternSequenceNode(1, SequencePatternKind.Paren, new PatternNode[] { new PatternCaptureNode(1, new IdNode(1, "x")) });
            AssertNodes(ast1, ast2, eq: false);
        }

        private static void AssertNodes(ASTNode ast1, ASTNode ast2, bool eq = true)
        {
            Assert.That(ast1, eq ? Is.EqualTo(ast2) : Is.Not.EqualTo(ast2));
            Assert.That(ast1 == ast2, Is.EqualTo(eq));
            Assert.That(ast1 != ast2, Is.EqualTo(!eq));
        }
    }
}
