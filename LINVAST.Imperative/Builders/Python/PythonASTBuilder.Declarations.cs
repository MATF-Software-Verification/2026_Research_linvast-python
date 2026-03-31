using System;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // import_stmt: import_name | import_from
        public override ASTNode VisitImport_stmt(Python3Parser.Import_stmtContext ctx) =>
            throw new NotImplementedException("import_stmt");

        // import_name: 'import' dotted_as_names
        public override ASTNode VisitImport_name(Python3Parser.Import_nameContext ctx) =>
            throw new NotImplementedException("import_name");

        // import_from: 'from' ... 'import' ...
        public override ASTNode VisitImport_from(Python3Parser.Import_fromContext ctx) =>
            throw new NotImplementedException("import_from");

        // import_as_name: name ('as' name)?
        public override ASTNode VisitImport_as_name(Python3Parser.Import_as_nameContext ctx) =>
            throw new NotImplementedException("import_as_name");

        // dotted_as_name: dotted_name ('as' name)?
        public override ASTNode VisitDotted_as_name(Python3Parser.Dotted_as_nameContext ctx) =>
            throw new NotImplementedException("dotted_as_name");

        // import_as_names: import_as_name (',' import_as_name)* ','?
        public override ASTNode VisitImport_as_names(Python3Parser.Import_as_namesContext ctx) =>
            throw new NotImplementedException("import_as_names");

        // dotted_as_names: dotted_as_name (',' dotted_as_name)*
        public override ASTNode VisitDotted_as_names(Python3Parser.Dotted_as_namesContext ctx) =>
            throw new NotImplementedException("dotted_as_names");

        // dotted_name: name ('.' name)*
        public override ASTNode VisitDotted_name(Python3Parser.Dotted_nameContext ctx) =>
            throw new NotImplementedException("dotted_name");

        // expr_stmt: testlist_star_expr (annassign | augassign ... | ('=' ...)*)
        public override ASTNode VisitExpr_stmt(Python3Parser.Expr_stmtContext ctx) =>
            throw new NotImplementedException("expr_stmt");

        // annassign: ':' test ('=' test)?
        public override ASTNode VisitAnnassign(Python3Parser.AnnassignContext ctx) =>
            throw new NotImplementedException("annassign");

        // augassign: '+=' | '-=' | '*=' | ...
        public override ASTNode VisitAugassign(Python3Parser.AugassignContext ctx) =>
            throw new NotImplementedException("augassign");

        // del_stmt: 'del' exprlist
        public override ASTNode VisitDel_stmt(Python3Parser.Del_stmtContext ctx) =>
            throw new NotImplementedException("del_stmt");

        // global_stmt: 'global' name (',' name)*
        public override ASTNode VisitGlobal_stmt(Python3Parser.Global_stmtContext ctx) =>
            throw new NotImplementedException("global_stmt");

        // nonlocal_stmt: 'nonlocal' name (',' name)*
        public override ASTNode VisitNonlocal_stmt(Python3Parser.Nonlocal_stmtContext ctx) =>
            throw new NotImplementedException("nonlocal_stmt");
    }
}
