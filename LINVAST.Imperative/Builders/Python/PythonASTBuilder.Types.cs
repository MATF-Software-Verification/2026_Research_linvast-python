using System.Collections.Generic;
using System.Linq;
using LINVAST.Builders;
using LINVAST.Imperative.Nodes;
using LINVAST.Nodes;

namespace LINVAST.Imperative.Builders.Python
{
    public sealed partial class PythonASTBuilder : Python3ParserBaseVisitor<ASTNode>, IASTBuilder<Python3Parser>
    {
        // classdef: 'class' name ('(' arglist? ')')? ':' block
        public override ASTNode VisitClassdef(Python3Parser.ClassdefContext ctx)
        {
            var className = new IdNode(ctx.Start.Line, ctx.name().GetText());

            var baseTypes = new TypeNameListNode(ctx.Start.Line);
            if (ctx.arglist() is not null) {
                var baseTypeNames = ctx.arglist().argument()
                    .Where(a => a.GetText().IndexOf('=') < 0)
                    .Select(a => new TypeNameNode(a.Start.Line, a.GetText()));
                baseTypes = new TypeNameListNode(ctx.Start.Line, baseTypeNames);
            }

            ASTNode body = this.Visit(ctx.block());
            IEnumerable<ASTNode> bodyChildren = body is BlockStatNode block
                ? block.Children
                : new[] { body };
            IReadOnlyList<ASTNode> processed = this.AddDeclarations(bodyChildren);

            var declarations = new List<DeclStatNode>();
            foreach (ASTNode child in processed) {
                if (child is DeclStatNode declStat)
                    declarations.Add(declStat);
                else if (child is FuncNode funcNode)
                    declarations.Add(funcNode);
                else
                    declarations.Add(new DeclStatNode(child.Line, new DeclSpecsNode(child.Line),
                        new DeclListNode(child.Line)));
            }

            var templateParams = new TypeNameListNode(ctx.Start.Line);
            var declSpecs = new DeclSpecsNode(ctx.Start.Line);
            var typeDecl = new TypeDeclNode(ctx.Start.Line, className, templateParams, baseTypes, declarations);
            return new ClassNode(ctx.Start.Line, declSpecs, typeDecl);
        }
    }
}
