using System;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // compound_stmt: if_stmt | while_stmt | for_stmt | try_stmt | with_stmt | funcdef | classdef | decorated | async_stmt | match_stmt
        public override ASTNode VisitCompound_stmt(Python3Parser.Compound_stmtContext ctx) =>
            throw new NotImplementedException("compound_stmt");

        // block: simple_stmts | NEWLINE INDENT stmt+ DEDENT
        public override ASTNode VisitBlock(Python3Parser.BlockContext ctx) =>
            throw new NotImplementedException("block");

        // if_stmt: 'if' test ':' block ('elif' test ':' block)* ('else' ':' block)?
        public override ASTNode VisitIf_stmt(Python3Parser.If_stmtContext ctx) =>
            throw new NotImplementedException("if_stmt");

        // while_stmt: 'while' test ':' block ('else' ':' block)?
        public override ASTNode VisitWhile_stmt(Python3Parser.While_stmtContext ctx) =>
            throw new NotImplementedException("while_stmt");

        // for_stmt: 'for' exprlist 'in' testlist ':' block ('else' ':' block)?
        public override ASTNode VisitFor_stmt(Python3Parser.For_stmtContext ctx) =>
            throw new NotImplementedException("for_stmt");

        // try_stmt: 'try' ':' block ((except_clause ':' block)+ ... | 'finally' ':' block)
        public override ASTNode VisitTry_stmt(Python3Parser.Try_stmtContext ctx) =>
            throw new NotImplementedException("try_stmt");

        // with_stmt: 'with' with_item (',' with_item)* ':' block
        public override ASTNode VisitWith_stmt(Python3Parser.With_stmtContext ctx) =>
            throw new NotImplementedException("with_stmt");

        // with_item: test ('as' expr)?
        public override ASTNode VisitWith_item(Python3Parser.With_itemContext ctx) =>
            throw new NotImplementedException("with_item");

        // except_clause: 'except' (test ('as' name)?)?
        public override ASTNode VisitExcept_clause(Python3Parser.Except_clauseContext ctx) =>
            throw new NotImplementedException("except_clause");

        // async_stmt: ASYNC (funcdef | with_stmt | for_stmt)
        public override ASTNode VisitAsync_stmt(Python3Parser.Async_stmtContext ctx) =>
            throw new NotImplementedException("async_stmt");

        // flow_stmt: break_stmt | continue_stmt | return_stmt | raise_stmt | yield_stmt
        public override ASTNode VisitFlow_stmt(Python3Parser.Flow_stmtContext ctx) =>
            throw new NotImplementedException("flow_stmt");

        // pass_stmt: 'pass'
        public override ASTNode VisitPass_stmt(Python3Parser.Pass_stmtContext ctx) =>
            throw new NotImplementedException("pass_stmt");

        // break_stmt: 'break'
        public override ASTNode VisitBreak_stmt(Python3Parser.Break_stmtContext ctx) =>
            throw new NotImplementedException("break_stmt");

        // continue_stmt: 'continue'
        public override ASTNode VisitContinue_stmt(Python3Parser.Continue_stmtContext ctx) =>
            throw new NotImplementedException("continue_stmt");

        // return_stmt: 'return' testlist?
        public override ASTNode VisitReturn_stmt(Python3Parser.Return_stmtContext ctx) =>
            throw new NotImplementedException("return_stmt");

        // raise_stmt: 'raise' (test ('from' test)?)?
        public override ASTNode VisitRaise_stmt(Python3Parser.Raise_stmtContext ctx) =>
            throw new NotImplementedException("raise_stmt");

        // yield_stmt: yield_expr
        public override ASTNode VisitYield_stmt(Python3Parser.Yield_stmtContext ctx) =>
            throw new NotImplementedException("yield_stmt");

        // assert_stmt: 'assert' test (',' test)?
        public override ASTNode VisitAssert_stmt(Python3Parser.Assert_stmtContext ctx) =>
            throw new NotImplementedException("assert_stmt");

        // match_stmt: 'match' subject_expr ':' NEWLINE INDENT case_block+ DEDENT
        public override ASTNode VisitMatch_stmt(Python3Parser.Match_stmtContext ctx) =>
            throw new NotImplementedException("match_stmt");

        // case_block: 'case' patterns guard? ':' block
        public override ASTNode VisitCase_block(Python3Parser.Case_blockContext ctx) =>
            throw new NotImplementedException("case_block");

        // subject_expr: star_named_expression ',' star_named_expressions? | test
        public override ASTNode VisitSubject_expr(Python3Parser.Subject_exprContext ctx) =>
            throw new NotImplementedException("subject_expr");

        // star_named_expressions: ',' star_named_expression+ ','?
        public override ASTNode VisitStar_named_expressions(Python3Parser.Star_named_expressionsContext ctx) =>
            throw new NotImplementedException("star_named_expressions");

        // star_named_expression: '*' expr | test
        public override ASTNode VisitStar_named_expression(Python3Parser.Star_named_expressionContext ctx) =>
            throw new NotImplementedException("star_named_expression");

        // guard: 'if' test
        public override ASTNode VisitGuard(Python3Parser.GuardContext ctx) =>
            throw new NotImplementedException("guard");

        // patterns: open_sequence_pattern | pattern
        public override ASTNode VisitPatterns(Python3Parser.PatternsContext ctx) =>
            throw new NotImplementedException("patterns");

        // pattern: as_pattern | or_pattern
        public override ASTNode VisitPattern(Python3Parser.PatternContext ctx) =>
            throw new NotImplementedException("pattern");

        // as_pattern: or_pattern 'as' pattern_capture_target
        public override ASTNode VisitAs_pattern(Python3Parser.As_patternContext ctx) =>
            throw new NotImplementedException("as_pattern");

        // or_pattern: closed_pattern ('|' closed_pattern)*
        public override ASTNode VisitOr_pattern(Python3Parser.Or_patternContext ctx) =>
            throw new NotImplementedException("or_pattern");

        // closed_pattern: literal_pattern | capture_pattern | wildcard_pattern | value_pattern | group_pattern | sequence_pattern | mapping_pattern | class_pattern
        public override ASTNode VisitClosed_pattern(Python3Parser.Closed_patternContext ctx) =>
            throw new NotImplementedException("closed_pattern");

        // literal_pattern: signed_number | complex_number | strings | 'None' | 'True' | 'False'
        public override ASTNode VisitLiteral_pattern(Python3Parser.Literal_patternContext ctx) =>
            throw new NotImplementedException("literal_pattern");

        // literal_expr: signed_number | complex_number | strings | 'None' | 'True' | 'False'
        public override ASTNode VisitLiteral_expr(Python3Parser.Literal_exprContext ctx) =>
            throw new NotImplementedException("literal_expr");

        // complex_number: signed_real_number ('+' | '-') imaginary_number
        public override ASTNode VisitComplex_number(Python3Parser.Complex_numberContext ctx) =>
            throw new NotImplementedException("complex_number");

        // signed_number: NUMBER | '-' NUMBER
        public override ASTNode VisitSigned_number(Python3Parser.Signed_numberContext ctx) =>
            throw new NotImplementedException("signed_number");

        // signed_real_number: real_number | '-' real_number
        public override ASTNode VisitSigned_real_number(Python3Parser.Signed_real_numberContext ctx) =>
            throw new NotImplementedException("signed_real_number");

        // real_number: NUMBER
        public override ASTNode VisitReal_number(Python3Parser.Real_numberContext ctx) =>
            throw new NotImplementedException("real_number");

        // imaginary_number: NUMBER
        public override ASTNode VisitImaginary_number(Python3Parser.Imaginary_numberContext ctx) =>
            throw new NotImplementedException("imaginary_number");

        // capture_pattern: pattern_capture_target
        public override ASTNode VisitCapture_pattern(Python3Parser.Capture_patternContext ctx) =>
            throw new NotImplementedException("capture_pattern");

        // pattern_capture_target: name
        public override ASTNode VisitPattern_capture_target(Python3Parser.Pattern_capture_targetContext ctx) =>
            throw new NotImplementedException("pattern_capture_target");

        // wildcard_pattern: '_'
        public override ASTNode VisitWildcard_pattern(Python3Parser.Wildcard_patternContext ctx) =>
            throw new NotImplementedException("wildcard_pattern");

        // value_pattern: attr
        public override ASTNode VisitValue_pattern(Python3Parser.Value_patternContext ctx) =>
            throw new NotImplementedException("value_pattern");

        // attr: name ('.' name)+
        public override ASTNode VisitAttr(Python3Parser.AttrContext ctx) =>
            throw new NotImplementedException("attr");

        // name_or_attr: attr | name
        public override ASTNode VisitName_or_attr(Python3Parser.Name_or_attrContext ctx) =>
            throw new NotImplementedException("name_or_attr");

        // group_pattern: '(' pattern ')'
        public override ASTNode VisitGroup_pattern(Python3Parser.Group_patternContext ctx) =>
            throw new NotImplementedException("group_pattern");

        // sequence_pattern: '[' maybe_sequence_pattern? ']' | '(' open_sequence_pattern? ')'
        public override ASTNode VisitSequence_pattern(Python3Parser.Sequence_patternContext ctx) =>
            throw new NotImplementedException("sequence_pattern");

        // open_sequence_pattern: maybe_star_pattern ',' maybe_sequence_pattern?
        public override ASTNode VisitOpen_sequence_pattern(Python3Parser.Open_sequence_patternContext ctx) =>
            throw new NotImplementedException("open_sequence_pattern");

        // maybe_sequence_pattern: maybe_star_pattern (',' maybe_star_pattern)* ','?
        public override ASTNode VisitMaybe_sequence_pattern(Python3Parser.Maybe_sequence_patternContext ctx) =>
            throw new NotImplementedException("maybe_sequence_pattern");

        // maybe_star_pattern: star_pattern | pattern
        public override ASTNode VisitMaybe_star_pattern(Python3Parser.Maybe_star_patternContext ctx) =>
            throw new NotImplementedException("maybe_star_pattern");

        // star_pattern: '*' pattern_capture_target | '*' wildcard_pattern
        public override ASTNode VisitStar_pattern(Python3Parser.Star_patternContext ctx) =>
            throw new NotImplementedException("star_pattern");

        // mapping_pattern: '{' ... '}'
        public override ASTNode VisitMapping_pattern(Python3Parser.Mapping_patternContext ctx) =>
            throw new NotImplementedException("mapping_pattern");

        // items_pattern: key_value_pattern (',' key_value_pattern)*
        public override ASTNode VisitItems_pattern(Python3Parser.Items_patternContext ctx) =>
            throw new NotImplementedException("items_pattern");

        // key_value_pattern: (literal_expr | attr) ':' pattern
        public override ASTNode VisitKey_value_pattern(Python3Parser.Key_value_patternContext ctx) =>
            throw new NotImplementedException("key_value_pattern");

        // double_star_pattern: '**' pattern_capture_target
        public override ASTNode VisitDouble_star_pattern(Python3Parser.Double_star_patternContext ctx) =>
            throw new NotImplementedException("double_star_pattern");

        // class_pattern: name_or_attr '(' ... ')'
        public override ASTNode VisitClass_pattern(Python3Parser.Class_patternContext ctx) =>
            throw new NotImplementedException("class_pattern");

        // positional_patterns: pattern (',' pattern)*
        public override ASTNode VisitPositional_patterns(Python3Parser.Positional_patternsContext ctx) =>
            throw new NotImplementedException("positional_patterns");

        // keyword_patterns: keyword_pattern (',' keyword_pattern)*
        public override ASTNode VisitKeyword_patterns(Python3Parser.Keyword_patternsContext ctx) =>
            throw new NotImplementedException("keyword_patterns");

        // keyword_pattern: name '=' pattern
        public override ASTNode VisitKeyword_pattern(Python3Parser.Keyword_patternContext ctx) =>
            throw new NotImplementedException("keyword_pattern");
    }
}
