using System;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // test: or_test ('if' or_test 'else' test)? | lambdef
        public override ASTNode VisitTest(Python3Parser.TestContext ctx) =>
            throw new NotImplementedException("test");

        // test_nocond: or_test | lambdef_nocond
        public override ASTNode VisitTest_nocond(Python3Parser.Test_nocondContext ctx) =>
            throw new NotImplementedException("test_nocond");

        // or_test: and_test ('or' and_test)*
        public override ASTNode VisitOr_test(Python3Parser.Or_testContext ctx) =>
            throw new NotImplementedException("or_test");

        // and_test: not_test ('and' not_test)*
        public override ASTNode VisitAnd_test(Python3Parser.And_testContext ctx) =>
            throw new NotImplementedException("and_test");

        // not_test: 'not' not_test | comparison
        public override ASTNode VisitNot_test(Python3Parser.Not_testContext ctx) =>
            throw new NotImplementedException("not_test");

        // comparison: expr (comp_op expr)*
        public override ASTNode VisitComparison(Python3Parser.ComparisonContext ctx) =>
            throw new NotImplementedException("comparison");

        // comp_op: '<' | '>' | '==' | '>=' | '<=' | '!=' | 'in' | 'not' 'in' | 'is' | 'is' 'not'
        public override ASTNode VisitComp_op(Python3Parser.Comp_opContext ctx) =>
            throw new NotImplementedException("comp_op");

        // expr: atom_expr | expr op expr | unary expr
        public override ASTNode VisitExpr(Python3Parser.ExprContext ctx) =>
            throw new NotImplementedException("expr");

        // star_expr: '*' expr
        public override ASTNode VisitStar_expr(Python3Parser.Star_exprContext ctx) =>
            throw new NotImplementedException("star_expr");

        // atom_expr: AWAIT? atom trailer*
        public override ASTNode VisitAtom_expr(Python3Parser.Atom_exprContext ctx) =>
            throw new NotImplementedException("atom_expr");

        // atom: '(' ... ')' | '[' ... ']' | '{' ... '}' | name | NUMBER | STRING+ | '...' | 'None' | 'True' | 'False'
        public override ASTNode VisitAtom(Python3Parser.AtomContext ctx) =>
            throw new NotImplementedException("atom");

        // name: NAME | '_' | 'match'
        public override ASTNode VisitName(Python3Parser.NameContext ctx) =>
            throw new NotImplementedException("name");

        // trailer: '(' arglist? ')' | '[' subscriptlist ']' | '.' name
        public override ASTNode VisitTrailer(Python3Parser.TrailerContext ctx) =>
            throw new NotImplementedException("trailer");

        // subscriptlist: subscript_ (',' subscript_)* ','?
        public override ASTNode VisitSubscriptlist(Python3Parser.SubscriptlistContext ctx) =>
            throw new NotImplementedException("subscriptlist");

        // subscript_: test | test? ':' test? sliceop?
        public override ASTNode VisitSubscript_(Python3Parser.Subscript_Context ctx) =>
            throw new NotImplementedException("subscript_");

        // sliceop: ':' test?
        public override ASTNode VisitSliceop(Python3Parser.SliceopContext ctx) =>
            throw new NotImplementedException("sliceop");

        // testlist_star_expr: (test | star_expr) (',' (test | star_expr))* ','?
        public override ASTNode VisitTestlist_star_expr(Python3Parser.Testlist_star_exprContext ctx) =>
            throw new NotImplementedException("testlist_star_expr");

        // testlist: test (',' test)* ','?
        public override ASTNode VisitTestlist(Python3Parser.TestlistContext ctx) =>
            throw new NotImplementedException("testlist");

        // exprlist: (expr | star_expr) (',' (expr | star_expr))* ','?
        public override ASTNode VisitExprlist(Python3Parser.ExprlistContext ctx) =>
            throw new NotImplementedException("exprlist");

        // testlist_comp: (test | star_expr) (comp_for | (',' (test | star_expr))* ','?)
        public override ASTNode VisitTestlist_comp(Python3Parser.Testlist_compContext ctx) =>
            throw new NotImplementedException("testlist_comp");

        // dictorsetmaker: ...
        public override ASTNode VisitDictorsetmaker(Python3Parser.DictorsetmakerContext ctx) =>
            throw new NotImplementedException("dictorsetmaker");

        // arglist: argument (',' argument)* ','?
        public override ASTNode VisitArglist(Python3Parser.ArglistContext ctx) =>
            throw new NotImplementedException("arglist");

        // argument: test comp_for? | test '=' test | '**' test | '*' test
        public override ASTNode VisitArgument(Python3Parser.ArgumentContext ctx) =>
            throw new NotImplementedException("argument");

        // comp_iter: comp_for | comp_if
        public override ASTNode VisitComp_iter(Python3Parser.Comp_iterContext ctx) =>
            throw new NotImplementedException("comp_iter");

        // comp_for: ASYNC? 'for' exprlist 'in' or_test comp_iter?
        public override ASTNode VisitComp_for(Python3Parser.Comp_forContext ctx) =>
            throw new NotImplementedException("comp_for");

        // comp_if: 'if' test_nocond comp_iter?
        public override ASTNode VisitComp_if(Python3Parser.Comp_ifContext ctx) =>
            throw new NotImplementedException("comp_if");

        // yield_expr: 'yield' yield_arg?
        public override ASTNode VisitYield_expr(Python3Parser.Yield_exprContext ctx) =>
            throw new NotImplementedException("yield_expr");

        // yield_arg: 'from' test | testlist
        public override ASTNode VisitYield_arg(Python3Parser.Yield_argContext ctx) =>
            throw new NotImplementedException("yield_arg");

        // strings: STRING+
        public override ASTNode VisitStrings(Python3Parser.StringsContext ctx) =>
            throw new NotImplementedException("strings");
    }
}
