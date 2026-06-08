using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Antlr4.Runtime.Misc;
using LINVAST.Builders;
using LINVAST.Exceptions;
using LINVAST.Imperative.Nodes;
using LINVAST.Imperative.Nodes.Common;
using LINVAST.Nodes;
using static LINVAST.Imperative.Builders.Java.JavaParser;

namespace LINVAST.Imperative.Builders.Java
{
    public sealed partial class JavaASTBuilder : JavaBaseVisitor<ASTNode>, IASTBuilder<JavaParser>
    {

        #region package and import declarations

        public override ASTNode VisitPackageDeclaration([NotNull] PackageDeclarationContext ctx)
            => new PackageNode(ctx.Start.Line, ctx.qualifiedName().GetText());

        public override ASTNode VisitImportDeclaration([NotNull] ImportDeclarationContext ctx)
        {
            string? qualifiedAs = null;
            if (ctx.STATIC() is not null)
                qualifiedAs = "static";

            var directive = new StringBuilder(this.Visit(ctx.qualifiedName()).As<IdNode>().GetText());
            if (ctx.MUL() is not null)
                directive.Append(".*");

            return new ImportNode(ctx.Start.Line, directive.ToString(), qualifiedAs);
        }

        #endregion

        #region class, enum, interface declarations

        public override ASTNode VisitClassDeclaration([NotNull] ClassDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            TypeNameListNode templateParams = ctx.typeParameters() is { } typeParamsCtx
                ? this.Visit(typeParamsCtx).As<TypeNameListNode>()
                : new TypeNameListNode(ctx.Start.Line);
            IEnumerable<TypeNameNode> baseTypes;
            int baseTypesStartLine = ctx.Start.Line;
            if (ctx.typeList() is { } typeListCtx) {
                baseTypes = this.Visit(typeListCtx).As<TypeNameListNode>().Types;
                baseTypesStartLine = typeListCtx.Start.Line;
            } else {
                baseTypes = new TypeNameNode[] { };
            }

            if (ctx.typeType() is { } typeTypeCtx) {
                baseTypes = baseTypes.Append(this.Visit(typeTypeCtx).As<TypeNameNode>());
                baseTypesStartLine = typeTypeCtx.Start.Line;
            }

            IEnumerable<DeclStatNode> declarations;
            BlockStatNode block = this.Visit(ctx.classBody()).As<BlockStatNode>();
            declarations = block.Children.Cast<DeclStatNode>();

            return new TypeDeclNode(ctx.Start.Line, identifier, templateParams,
                new TypeNameListNode(baseTypesStartLine, baseTypes),
                declarations);
        }

        public override ASTNode VisitEnumDeclaration([NotNull] EnumDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            var constantsNode = new DeclListNode(ctx.Start.Line);
            if (ctx.enumConstants() is not null) {
                IEnumerable<DeclNode> constants = ctx.enumConstants().enumConstant().Select(c => this.Visit(c).As<DeclNode>());
                constantsNode = new DeclListNode(ctx.Start.Line, constants);
            }

            IEnumerable<ASTNode>? enumBodyNodes = null;
            if (ctx.enumBodyDeclarations() is not null) {
                enumBodyNodes = ctx.enumBodyDeclarations().classBodyDeclaration().Select(this.VisitClassBodyDeclaration);
            }
                
            return enumBodyNodes is null
                ? new EnumDeclNode(ctx.Start.Line, identifier, constantsNode) 
                : new EnumDeclNode(ctx.Start.Line, identifier, constantsNode, enumBodyNodes);
        }

        public override ASTNode VisitEnumConstant([NotNull] EnumConstantContext ctx)
        {
            int line = ctx.Start.Line;

            IEnumerable<TagNode>? annotations = null;
            if (ctx.annotation() is not null) {
                annotations = ctx.annotation().Select(a => this.Visit(a).As<TagNode>());
            }
                
            ExprNode? initializer = ctx.arguments() is not null
                ? new FuncCallExprNode(line, new IdNode(line, ctx.IDENTIFIER().GetText()), this.Visit(ctx.arguments()).As<ExprListNode>())
                : null;
            
            if (annotations is null)
                return initializer is null
                    ? new VarDeclNode(line, new IdNode(line, ctx.IDENTIFIER().GetText()))
                    : new VarDeclNode(line, new IdNode(line, ctx.IDENTIFIER().GetText()), initializer);

            return initializer is null
                ? new VarDeclNode(line, annotations, new IdNode(line, ctx.IDENTIFIER().GetText()))
                : new VarDeclNode(line, annotations, new IdNode(line, ctx.IDENTIFIER().GetText()), initializer);
        }

        public override ASTNode VisitEnumBodyDeclarations([NotNull] EnumBodyDeclarationsContext ctx)
            => new BlockStatNode(ctx.Start.Line, ctx.classBodyDeclaration().Select(this.VisitClassBodyDeclaration));

        public override ASTNode VisitInterfaceDeclaration([NotNull] InterfaceDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            TypeNameListNode? templateParams = null;
            if (ctx.typeParameters() is not null)
                templateParams = this.Visit(ctx.typeParameters()).As<TypeNameListNode>();

            TypeNameListNode? baseTypes = null;
            if (ctx.typeList() is not null)
                baseTypes = this.Visit(ctx.typeList()).As<TypeNameListNode>();

            IEnumerable<DeclStatNode> declarations;
            BlockStatNode block = this.Visit(ctx.interfaceBody()).As<BlockStatNode>();
            declarations = block.Children.Select(c => c.As<DeclStatNode>());

            return new TypeDeclNode(ctx.Start.Line, identifier,
                templateParams ?? new TypeNameListNode(ctx.Start.Line),
                baseTypes ?? new TypeNameListNode(ctx.Start.Line),
                declarations);
        }

        public override ASTNode VisitClassBodyDeclaration([NotNull] ClassBodyDeclarationContext ctx)
        {
            if (ctx.SEMI() is not null && ctx.ChildCount == 1)
                return new EmptyStatNode(ctx.Start.Line);

            if (ctx.STATIC() is not null || ctx.block() is not null)
                return this.Visit(ctx.block()).As<BlockStatNode>();

            // we use private method ProcessModifier instead of overriding VisitModifier
            string modifiers = "";
            IEnumerable<TagNode> tags = Enumerable.Empty<TagNode>();
            if (ctx.modifier() is { } modifierCtxList && modifierCtxList.Any()) {
                tags = modifierCtxList.Select(this.ProcessModifierTag).Where(tag => tag is not null).Cast<TagNode>();
                modifiers = string.Join(" ", modifierCtxList
                    .Select(modCtx => this.ProcessModifier(modCtx))
                    .Where(mod => !string.IsNullOrWhiteSpace(mod)));
            }

            MemberDeclarationContext memberDeclCtx = ctx.memberDeclaration();
            TypeNameNode typeName = TypeName(memberDeclCtx);
            var declSpecs = new DeclSpecsNode(typeName.Line, modifiers, typeName);
            DeclListNode declList = memberDeclCtx.fieldDeclaration() is not null
                ? this.Visit(memberDeclCtx).As<DeclListNode>()
                : new DeclListNode(memberDeclCtx.Start.Line, this.Visit(memberDeclCtx).As<DeclNode>());
            
            return tags.Any()
                ? new DeclStatNode(ctx.Start.Line, tags, declSpecs, declList)
                : new DeclStatNode(ctx.Start.Line, declSpecs, declList);

            TypeNameNode TypeName([NotNull] MemberDeclarationContext ctx)
            {
                if (ctx.classDeclaration() is { } clsDeclCtx)
                    return new TypeNameNode(clsDeclCtx.Start.Line, clsDeclCtx.IDENTIFIER().GetText());

                if (ctx.interfaceDeclaration() is { } interfaceDeclCtx)
                    return new TypeNameNode(interfaceDeclCtx.Start.Line, interfaceDeclCtx.IDENTIFIER().GetText());

                if (ctx.enumDeclaration() is { } enumDeclCtx)
                    return new TypeNameNode(enumDeclCtx.Start.Line, enumDeclCtx.IDENTIFIER().GetText());

                if (ctx.annotationTypeDeclaration() is { } annotationDeclCtx)
                    return new TypeNameNode(annotationDeclCtx.Start.Line, annotationDeclCtx.IDENTIFIER().GetText());

                if (ctx.methodDeclaration() is { } methodDeclCtx)
                    return AppendArrayBrackets(this.Visit(methodDeclCtx.typeTypeOrVoid()).As<TypeNameNode>(), methodDeclCtx.LBRACK().Length);

                if (ctx.genericMethodDeclaration() is { } genMethDeclCtx)
                    return AppendArrayBrackets(
                        this.Visit(genMethDeclCtx.methodDeclaration().typeTypeOrVoid()).As<TypeNameNode>(),
                        genMethDeclCtx.methodDeclaration().LBRACK().Length);

                if (ctx.fieldDeclaration() is { } fieldDeclCtx)
                    return this.Visit(fieldDeclCtx.typeType()).As<TypeNameNode>();

                if (ctx.constructorDeclaration() is not null) {
                    ConstructorDeclarationContext cctx = ctx.constructorDeclaration();
                    return new TypeNameNode(ctx.Start.Line, cctx.IDENTIFIER().GetText());
                }

                if (ctx.genericConstructorDeclaration() is not null) {
                    ConstructorDeclarationContext cctx = ctx.genericConstructorDeclaration().constructorDeclaration();
                    return new TypeNameNode(ctx.Start.Line, cctx.IDENTIFIER().GetText());
                }

                // unreachable path
                throw new SyntaxErrorException("Source file contained unexpected content");
            }
        }

        #endregion

        #region class member declarations

        public override ASTNode VisitMemberDeclaration([NotNull] MemberDeclarationContext ctx)
            => this.Visit(ctx.children.Single());

        public override ASTNode VisitMethodDeclaration([NotNull] MethodDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            FuncParamsNode @params = this.Visit(ctx.formalParameters()).As<FuncParamsNode>();

            BlockStatNode body = this.Visit(ctx.methodBody()).As<BlockStatNode>();

            return new FuncDeclNode(ctx.Start.Line, identifier, @params, body);
        }

        public override ASTNode VisitGenericMethodDeclaration([NotNull] GenericMethodDeclarationContext ctx)
        {
            TypeNameListNode templateArgs = this.Visit(ctx.typeParameters()).As<TypeNameListNode>();
            FuncDeclNode func = this.Visit(ctx.methodDeclaration()).As<FuncDeclNode>();

            // throwing exception to suppress warnings
            return new FuncDeclNode(ctx.Start.Line, func.IdentifierNode, templateArgs,
                    func.ParametersNode ?? throw new SyntaxErrorException("Unknown construct"),
                    func.Definition ?? throw new SyntaxErrorException("Unknown construct"));
        }

        public override ASTNode VisitGenericConstructorDeclaration([NotNull] GenericConstructorDeclarationContext ctx)
        {
            TypeNameListNode templateArgs = this.Visit(ctx.typeParameters()).As<TypeNameListNode>();
            FuncDeclNode ctorDecl = this.VisitConstructorDeclaration(ctx.constructorDeclaration()).As<FuncDeclNode>();
            return new FuncDeclNode(ctx.Start.Line, ctorDecl.IdentifierNode, templateArgs, ctorDecl.Definition);
        }

        public override ASTNode VisitConstructorDeclaration([NotNull] ConstructorDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());
            FuncParamsNode @params = this.Visit(ctx.formalParameters()).As<FuncParamsNode>();

            var body = new BlockStatNode(ctx.Start.Line, ctx.constructorBody.blockStatement().Select(this.Visit));
            return new FuncDeclNode(ctx.Start.Line, identifier, @params, body);
        }

        public override ASTNode VisitFieldDeclaration([NotNull] FieldDeclarationContext ctx)
            => this.Visit(ctx.variableDeclarators()); // DeclListNode

        #endregion

        #region interface member declarations

        public override ASTNode VisitInterfaceBodyDeclaration([NotNull] InterfaceBodyDeclarationContext ctx)
        {
            if (ctx.SEMI() is not null && ctx.ChildCount == 1)
                return new EmptyStatNode(ctx.Start.Line);

            var modifiers = new StringBuilder("");
            int? declSpecsStartLine = null;
            var tags = new List<TagNode>();
            if (ctx.modifier() is { } modifierCtxList && modifierCtxList.Any()) {
                tags.AddRange(modifierCtxList.Select(this.ProcessModifierTag).Where(tag => tag is not null).Cast<TagNode>());
                modifiers.Append(string.Join(" ",
                    modifierCtxList
                        .Select(modCtx => this.ProcessModifier(modCtx))
                        .Where(mod => !string.IsNullOrWhiteSpace(mod))));
                declSpecsStartLine = modifierCtxList.First().Start.Line;
            }

            // additionally, there could be interface specific method modifiers
            if (ctx.interfaceMemberDeclaration().interfaceMethodDeclaration() is { } iMethodDeclCtx &&
                iMethodDeclCtx.interfaceMethodModifier() is { } iMethodModCtxList &&
                iMethodModCtxList.Any()) {
                tags.AddRange(iMethodModCtxList
                    .Where(iMCtx => iMCtx.annotation() is not null)
                    .Select(iMCtx => this.Visit(iMCtx.annotation()).As<TagNode>()));
                if (modifiers.Length > 0)
                    modifiers.Append(" ");
                modifiers.Append(string.Join(" ",
                    iMethodModCtxList
                        .Select(iMCtx => this.ProcessInterfaceMethodModifier(iMCtx))
                        .Where(mod => !string.IsNullOrWhiteSpace(mod))));
                declSpecsStartLine ??= iMethodModCtxList.First().Start.Line;
            }

            TypeNameNode type = TypeName(ctx.interfaceMemberDeclaration());
            declSpecsStartLine ??= type.Line;
            var declSpecs = new DeclSpecsNode(declSpecsStartLine ?? ctx.Start.Line, modifiers.ToString(), type);

            DeclListNode declList = ctx.interfaceMemberDeclaration().constDeclaration() is { } constDeclCtx
                ? this.Visit(constDeclCtx).As<DeclListNode>()
                : new DeclListNode(ctx.interfaceMemberDeclaration().Start.Line,
                    this.Visit(ctx.interfaceMemberDeclaration()).As<DeclNode>());
            return tags.Any()
                ? new DeclStatNode(ctx.Start.Line, tags, declSpecs, declList)
                : new DeclStatNode(ctx.Start.Line, declSpecs, declList);


            TypeNameNode TypeName([NotNull] InterfaceMemberDeclarationContext ctx)
            {
                if (ctx.constDeclaration() is { } constDeclCtx)
                    return this.Visit(constDeclCtx.typeType()).As<TypeNameNode>();

                if (ctx.interfaceMethodDeclaration() is { } iMethodDeclCtx)
                    return AppendArrayBrackets(this.Visit(iMethodDeclCtx.typeTypeOrVoid()).As<TypeNameNode>(), iMethodDeclCtx.LBRACK().Length);

                if (ctx.genericInterfaceMethodDeclaration() is { } genIntMethdDeclCtx)
                    return AppendArrayBrackets(
                        this.Visit(genIntMethdDeclCtx.interfaceMethodDeclaration().typeTypeOrVoid()).As<TypeNameNode>(),
                        genIntMethdDeclCtx.interfaceMethodDeclaration().LBRACK().Length);

                if (ctx.interfaceDeclaration() is { } interfaceDeclCtx)
                    return new TypeNameNode(interfaceDeclCtx.Start.Line, interfaceDeclCtx.IDENTIFIER().GetText());

                if (ctx.annotationTypeDeclaration() is { } annotationDeclCtx)
                    return new TypeNameNode(annotationDeclCtx.Start.Line, annotationDeclCtx.IDENTIFIER().GetText());

                if (ctx.classDeclaration() is { } clsDeclCtx)
                    return new TypeNameNode(clsDeclCtx.Start.Line, clsDeclCtx.IDENTIFIER().GetText());

                if (ctx.enumDeclaration() is { } enumDeclCtx)
                    return new TypeNameNode(enumDeclCtx.Start.Line, enumDeclCtx.IDENTIFIER().GetText());

                // unreachable path
                throw new SyntaxErrorException("Source file contained unexpected content");
            }

        }

        public override ASTNode VisitInterfaceMemberDeclaration([NotNull] InterfaceMemberDeclarationContext ctx)
            => base.Visit(ctx.children.Single());

        public override ASTNode VisitConstDeclaration([NotNull] ConstDeclarationContext ctx)
            => new DeclListNode(ctx.Start.Line, ctx.constantDeclarator().Select(
                constDeclCtx => this.Visit(constDeclCtx).As<DeclNode>()));

        public override ASTNode VisitConstantDeclarator([NotNull] ConstantDeclaratorContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            ExprNode init = this.Visit(ctx.variableInitializer()).As<ExprNode>();
            if (ctx.LBRACK().Length > 0 && init is ArrInitExprNode arrInit)
                return new ArrDeclNode(ctx.Start.Line, identifier, arrInit);
            if (ctx.LBRACK().Length > 0)
                return new ArrDeclNode(ctx.Start.Line, identifier);

            return new VarDeclNode(ctx.Start.Line, identifier, init);
        }

        public override ASTNode VisitInterfaceMethodDeclaration([NotNull] InterfaceMethodDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

            TypeNameListNode? templateArgs = null;
            if (ctx.typeParameters() is not null)
                templateArgs = this.Visit(ctx.typeParameters()).As<TypeNameListNode>();

            FuncParamsNode @params = this.Visit(ctx.formalParameters()).As<FuncParamsNode>();

            BlockStatNode body = this.Visit(ctx.methodBody()).As<BlockStatNode>();

            return new FuncDeclNode(ctx.Start.Line, identifier,
                templateArgs ?? new TypeNameListNode(ctx.Start.Line),
                @params, body);
        }

        public override ASTNode VisitGenericInterfaceMethodDeclaration([NotNull] GenericInterfaceMethodDeclarationContext ctx)
        {
            TypeNameListNode templateArgs = this.Visit(ctx.typeParameters()).As<TypeNameListNode>();
            FuncDeclNode func = this.Visit(ctx.interfaceMethodDeclaration()).As<FuncDeclNode>();

            // throwing exception to suppress warning
            return new FuncDeclNode(ctx.Start.Line, func.IdentifierNode,
                new TypeNameListNode(templateArgs.Line, templateArgs.Types.Concat(func.TemplateArgs.Types)),
                func.ParametersNode ?? throw new SyntaxErrorException("Unknown construct"),
                func.Definition ?? throw new SyntaxErrorException("Unknown construct"));
        }

        #endregion

        #region variable declarators

        public override ASTNode VisitVariableDeclarators([NotNull] VariableDeclaratorsContext ctx)
            => new DeclListNode(ctx.Start.Line, ctx.variableDeclarator().Select(
                varDeclCtx => this.Visit(varDeclCtx).As<DeclNode>()));

        public override ASTNode VisitVariableDeclarator([NotNull] VariableDeclaratorContext ctx)
        {
            VariableDeclaratorIdContext varDeclIdCtx = ctx.variableDeclaratorId();
            IdNode identifier = this.Visit(varDeclIdCtx).As<IdNode>();
            bool isArray = varDeclIdCtx.LBRACK().Length > 0;

            if (ctx.variableInitializer() is { } varInitCtx) {
                ExprNode init = this.Visit(varInitCtx).As<ExprNode>();
                if (isArray && init is ArrInitExprNode arrInit)
                    return new ArrDeclNode(ctx.Start.Line, identifier, arrInit);
                if (isArray)
                    return new ArrDeclNode(ctx.Start.Line, identifier);
                return new VarDeclNode(ctx.Start.Line, identifier, init);
            }

            if (isArray)
                return new ArrDeclNode(ctx.Start.Line, identifier);

            return new VarDeclNode(ctx.Start.Line, identifier);
        }

        public override ASTNode VisitVariableDeclaratorId([NotNull] VariableDeclaratorIdContext ctx)
            => new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());

        #endregion

        #region local declarations

        public override ASTNode VisitLocalVariableDeclaration([NotNull] LocalVariableDeclarationContext ctx)
        {
            string modifiers = "";
            int? declSpecsStartLine = null;
            IEnumerable<TagNode> tags = Enumerable.Empty<TagNode>();
            if (ctx.variableModifier() is { } varModifierCtxList && varModifierCtxList.Any()) {
                tags = varModifierCtxList
                    .Where(modCtx => modCtx.annotation() is not null)
                    .Select(modCtx => this.Visit(modCtx.annotation()).As<TagNode>());
                modifiers = string.Join(" ",
                    varModifierCtxList
                        .Select(modCtx => this.ProcessVariableModifier(modCtx))
                        .Where(mod => !string.IsNullOrWhiteSpace(mod)));
                declSpecsStartLine = varModifierCtxList.First().Start.Line;
            }

            TypeNameNode typeName = this.Visit(ctx.typeType()).As<TypeNameNode>();
            declSpecsStartLine ??= typeName.Line;
            var declSpecs = new DeclSpecsNode(declSpecsStartLine ?? ctx.Start.Line, modifiers, typeName);
            DeclListNode declList = this.Visit(ctx.variableDeclarators()).As<DeclListNode>();

            return tags.Any()
                ? new DeclStatNode(ctx.Start.Line, tags, declSpecs, declList)
                : new DeclStatNode(ctx.Start.Line, declSpecs, declList);
        }

        public override ASTNode VisitLocalTypeDeclaration([NotNull] LocalTypeDeclarationContext ctx)
        {
            if (ctx.SEMI() is not null)
                return new EmptyStatNode(ctx.Start.Line);

            string modifiers = "";
            int? declSpecsStartLine = null;
            if (ctx.classOrInterfaceModifier() is { } clssOrInterfaceModList &&
                clssOrInterfaceModList.Any()) {
                modifiers = string.Join(" ", clssOrInterfaceModList.Select(
                    modCtx => this.ProcessClassOrInterfaceModifier(modCtx)));
                declSpecsStartLine = clssOrInterfaceModList.First().Start.Line;
            }

            if (ctx.classDeclaration() is not null) {
                TypeDeclNode decl = this.Visit(ctx.classDeclaration()).As<TypeDeclNode>();
                declSpecsStartLine ??= decl.Line;
                var declSpecs = new DeclSpecsNode(declSpecsStartLine ?? ctx.Start.Line, modifiers, decl.Identifier);
                return new ClassNode(ctx.Start.Line, declSpecs, decl);
            }

            if (ctx.interfaceDeclaration() is not null) {
                TypeDeclNode decl = this.Visit(ctx.interfaceDeclaration()).As<TypeDeclNode>();
                declSpecsStartLine ??= decl.Line;
                var declSpecs = new DeclSpecsNode(declSpecsStartLine ?? ctx.Start.Line, modifiers, decl.Identifier);
                return new InterfaceNode(ctx.Start.Line, declSpecs, decl);
            }

            // unreachable path
            throw new SyntaxErrorException("Source file contained unexpected content");
        }

        #endregion

        #region annotation declarations

        public override ASTNode VisitAnnotationTypeDeclaration([NotNull] AnnotationTypeDeclarationContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());
            BlockStatNode block = this.Visit(ctx.annotationTypeBody()).As<BlockStatNode>();
            return new TypeDeclNode(
                ctx.Start.Line,
                identifier,
                new TypeNameListNode(ctx.Start.Line),
                new TypeNameListNode(ctx.Start.Line),
                block.Children.OfType<DeclStatNode>());
        }

        public override ASTNode VisitAnnotationTypeBody([NotNull] AnnotationTypeBodyContext ctx)
            => new BlockStatNode(ctx.Start.Line, ctx.annotationTypeElementDeclaration().Select(this.Visit));

        public override ASTNode VisitAnnotationTypeElementDeclaration([NotNull] AnnotationTypeElementDeclarationContext ctx)
        {
            if (ctx.SEMI() is not null)
                return new EmptyStatNode(ctx.Start.Line);

            string modifiers = string.Join(" ", ctx.modifier().Select(this.ProcessModifier).Where(mod => !string.IsNullOrWhiteSpace(mod)));
            return CreateAnnotationElementDeclaration(ctx.Start.Line, modifiers, ctx.annotationTypeElementRest());
        }

        private DeclStatNode CreateAnnotationElementDeclaration(int line, string modifiers, [NotNull] AnnotationTypeElementRestContext ctx)
        {
            if (ctx.typeType() is not null) {
                TypeNameNode typeName = this.Visit(ctx.typeType()).As<TypeNameNode>();
                DeclListNode declList = CreateAnnotationMethodOrConstantList(ctx.annotationMethodOrConstantRest());
                return new DeclStatNode(line, new DeclSpecsNode(typeName.Line, modifiers, typeName), declList);
            }

            DeclNode decl = this.Visit(ctx.children.First()).As<DeclNode>();
            return new DeclStatNode(line, new DeclSpecsNode(ctx.Start.Line, modifiers, decl.Identifier), new DeclListNode(ctx.Start.Line, decl));
        }

        private DeclListNode CreateAnnotationMethodOrConstantList([NotNull] AnnotationMethodOrConstantRestContext ctx)
        {
            if (ctx.annotationConstantRest() is not null)
                return this.Visit(ctx.annotationConstantRest().variableDeclarators()).As<DeclListNode>();

            AnnotationMethodRestContext method = ctx.annotationMethodRest();
            var identifier = new IdNode(method.Start.Line, method.IDENTIFIER().GetText());
            var decl = new FuncDeclNode(method.Start.Line, identifier, new FuncParamsNode(method.Start.Line), new BlockStatNode(method.Start.Line));
            return new DeclListNode(method.Start.Line, decl);
        }

        #endregion

        #region private methods instead of visiting Modifier Contexts

        private static TypeNameNode AppendArrayBrackets(TypeNameNode type, int bracketCount)
            => bracketCount == 0
                ? type
                : new TypeNameNode(type.Line, $"{type.TypeName}{string.Concat(Enumerable.Repeat("[]", bracketCount))}", type.TemplateArguments);

        private string ProcessModifier(ModifierContext modifierCtx)
        {
            if (modifierCtx.classOrInterfaceModifier() is { } classOrInterfaceModCtx)
                return this.ProcessClassOrInterfaceModifier(classOrInterfaceModCtx);

            return modifierCtx.GetText();
        }

        private string ProcessClassOrInterfaceModifier([NotNull] ClassOrInterfaceModifierContext classOrInterfaceModCtx)
        {
            if (classOrInterfaceModCtx.annotation() is not null)
                return "";

            return classOrInterfaceModCtx.GetText();
        }

        private string ProcessInterfaceMethodModifier([NotNull] InterfaceMethodModifierContext interfaceMethodModCtx)
        {
            if (interfaceMethodModCtx.annotation() is not null)
                return "";

            return interfaceMethodModCtx.GetText();
        }

        private string ProcessVariableModifier(VariableModifierContext variableModifierCtx)
        {
            if (variableModifierCtx.annotation() is not null)
                return "";

            return variableModifierCtx.GetText();
        }

        private TagNode? ProcessModifierTag(ModifierContext modifierCtx)
        {
            if (modifierCtx.classOrInterfaceModifier()?.annotation() is { } annotationCtx)
                return this.Visit(annotationCtx).As<TagNode>();

            return null;
        }

        #endregion

        #region other (overriden just for the purposes of testing the above methods)

        public override ASTNode VisitQualifiedName([NotNull] QualifiedNameContext ctx)
            => new IdNode(ctx.Start.Line,
                string.Join('.', ctx.IDENTIFIER().Select(id => id.GetText())));

        public override ASTNode VisitBlock([NotNull] BlockContext ctx)
            => new BlockStatNode(ctx.Start.Line, ctx.blockStatement().Select(s => this.Visit(s)));

        public override ASTNode VisitBlockStatement([NotNull] BlockStatementContext ctx)
        {
            if (ctx.localVariableDeclaration() is not null)
                return this.Visit(ctx.localVariableDeclaration());

            if (ctx.statement() is not null)
                return this.Visit(ctx.statement());

            return this.Visit(ctx.localTypeDeclaration());
        }

        public override ASTNode VisitStatement([NotNull] StatementContext ctx)
        {
            if (ctx.blockLabel is not null)
                return this.Visit(ctx.blockLabel);

            if (ctx.ASSERT() is not null)
                return this.MarkerStatement(ctx.Start.Line, "__linvast_assert", ctx.expression().Select(e => this.Visit(e).As<ExprNode>()).ToArray());

            if (ctx.RETURN() is not null) {
                ExprNode? expr = ctx.expression().Length > 0 ? this.Visit(ctx.expression().Last()).As<ExprNode>() : null;
                return new JumpStatNode(ctx.Start.Line, expr);
            }

            if (ctx.THROW() is not null)
                return new ThrowStatNode(ctx.Start.Line, this.Visit(ctx.expression().Single()).As<ExprNode>());

            if (ctx.BREAK() is not null)
                return new JumpStatNode(ctx.Start.Line, JumpStatType.Break);

            if (ctx.CONTINUE() is not null)
                return new JumpStatNode(ctx.Start.Line, JumpStatType.Continue);

            if (ctx.statementExpression is not null)
                return new ExprStatNode(ctx.Start.Line, this.Visit(ctx.statementExpression).As<ExprNode>());

            if (ctx.SEMI() is not null && ctx.ChildCount == 1)
                return new EmptyStatNode(ctx.Start.Line);

            if (ctx.IF() is not null) {
                ExprNode condition = this.Visit(ctx.parExpression()).As<ExprNode>();
                StatementContext[] statements = ctx.statement();
                StatNode thenStatement = this.Visit(statements.First()).As<StatNode>();
                StatNode? elseStatement = statements.Length > 1 ? this.Visit(statements.Last()).As<StatNode>() : null;
                return elseStatement is null
                    ? new IfStatNode(ctx.Start.Line, condition, thenStatement)
                    : new IfStatNode(ctx.Start.Line, condition, thenStatement, elseStatement);
            }

            if (ctx.FOR() is not null) {
                StatNode body = this.Visit(ctx.statement().Single()).As<StatNode>();
                ForControlContext forControl = ctx.forControl();
                if (forControl.enhancedForControl() is not null) {
                    EnhancedForControlContext enhancedFor = forControl.enhancedForControl();
                    DeclStatNode iteratorDeclaration = this.EnhancedForIteratorDeclaration(enhancedFor);
                    ExprNode iterable = this.Visit(enhancedFor.expression()).As<ExprNode>();
                    return new ForeachStatNode(ctx.Start.Line, iteratorDeclaration, iterable, body);
                }

                ExprNode? init = forControl.forInit() is not null
                    ? this.ForInitExpression(forControl.forInit())
                    : null;
                ExprNode? condition = forControl.expression() is not null
                    ? this.Visit(forControl.expression()).As<ExprNode>()
                    : null;
                ExprNode? update = forControl.forUpdate is not null
                    ? this.ExpressionListExpression(forControl.forUpdate)
                    : null;
                return new ForStatNode(ctx.Start.Line, init, condition, update, body);
            }

            if (ctx.DO() is not null) {
                StatNode firstRun = this.Visit(ctx.statement().Single()).As<StatNode>();
                StatNode loopBody = this.Visit(ctx.statement().Single()).As<StatNode>();
                ExprNode condition = this.Visit(ctx.parExpression()).As<ExprNode>();
                return new BlockStatNode(ctx.Start.Line, firstRun, new WhileStatNode(ctx.Start.Line, condition, loopBody));
            }

            if (ctx.WHILE() is not null) {
                ExprNode condition = this.Visit(ctx.parExpression()).As<ExprNode>();
                StatNode statement = this.Visit(ctx.statement().Single()).As<StatNode>();
                return new WhileStatNode(ctx.Start.Line, condition, statement);
            }

            if (ctx.TRY() is not null) {
                var parts = new List<ASTNode>();
                if (ctx.resourceSpecification() is not null)
                    parts.Add(this.MarkerStatement(ctx.Start.Line, "__linvast_try_resources", new IdNode(ctx.resourceSpecification().Start.Line, ctx.resourceSpecification().GetText())));
                else
                    parts.Add(this.MarkerStatement(ctx.Start.Line, "__linvast_try"));

                parts.Add(this.Visit(ctx.block()).As<BlockStatNode>());
                parts.AddRange(ctx.catchClause().Select(this.Visit));
                if (ctx.finallyBlock() is not null)
                    parts.Add(this.Visit(ctx.finallyBlock()));

                return new BlockStatNode(ctx.Start.Line, parts);
            }

            if (ctx.SWITCH() is not null) {
                ExprNode condition = this.Visit(ctx.parExpression()).As<ExprNode>();
                IEnumerable<ASTNode> groups = ctx.switchBlockStatementGroup().Select(this.Visit);
                IEnumerable<ASTNode> trailingLabels = ctx.switchLabel()
                    .Select(label => new LabeledStatNode(
                        label.Start.Line,
                        this.SwitchLabelText(label),
                        new EmptyStatNode(label.Start.Line)));
                return new SwitchStatNode(ctx.Start.Line, condition, new BlockStatNode(ctx.Start.Line, groups.Concat(trailingLabels)));
            }

            if (ctx.SYNCHRONIZED() is not null) {
                ExprNode condition = this.Visit(ctx.parExpression()).As<ExprNode>();
                BlockStatNode body = this.Visit(ctx.block()).As<BlockStatNode>();
                return new BlockStatNode(ctx.Start.Line, this.MarkerStatement(ctx.Start.Line, "__linvast_synchronized", condition), body);
            }

            if (ctx.identifierLabel is not null)
                return new LabeledStatNode(ctx.Start.Line, ctx.identifierLabel.Text, this.Visit(ctx.statement().Single()).As<StatNode>());

            throw new NotImplementedException($"Java statement: {ctx.Start.Text}");
        }

        public override ASTNode VisitCatchClause([NotNull] CatchClauseContext ctx)
            => new LabeledStatNode(
                ctx.Start.Line,
                $"catch {ctx.catchType().GetText()} {ctx.IDENTIFIER().GetText()}",
                this.Visit(ctx.block()).As<BlockStatNode>());

        public override ASTNode VisitCatchType([NotNull] CatchTypeContext ctx)
            => new TypeNameNode(ctx.Start.Line, ctx.GetText());

        public override ASTNode VisitFinallyBlock([NotNull] FinallyBlockContext ctx)
            => new LabeledStatNode(ctx.Start.Line, "finally", this.Visit(ctx.block()).As<BlockStatNode>());

        public override ASTNode VisitSwitchBlockStatementGroup([NotNull] SwitchBlockStatementGroupContext ctx)
        {
            StatNode statement = new BlockStatNode(ctx.Start.Line, ctx.blockStatement().Select(this.Visit));
            foreach (SwitchLabelContext label in ctx.switchLabel().Reverse())
                statement = new LabeledStatNode(label.Start.Line, this.SwitchLabelText(label), statement);

            return statement;
        }

        public override ASTNode VisitSwitchLabel([NotNull] SwitchLabelContext ctx)
            => new LabeledStatNode(ctx.Start.Line, this.SwitchLabelText(ctx), new EmptyStatNode(ctx.Start.Line));

        public override ASTNode VisitClassBody([NotNull] ClassBodyContext ctx)
            => new BlockStatNode(ctx.Start.Line);

        public override ASTNode VisitInterfaceBody([NotNull] InterfaceBodyContext ctx)
            => new BlockStatNode(ctx.Start.Line);

        public override ASTNode VisitMethodBody([NotNull] MethodBodyContext ctx)
            => ctx.block() is not null ? this.Visit(ctx.block()) : new BlockStatNode(ctx.Start.Line);

        public override ASTNode VisitFormalParameters([NotNull] FormalParametersContext ctx)
            => new FuncParamsNode(ctx.Start.Line);

        private string SwitchLabelText([NotNull] SwitchLabelContext ctx)
        {
            if (ctx.DEFAULT() is not null)
                return "default";

            if (ctx.constantExpression is not null)
                return $"case {this.Visit(ctx.constantExpression).As<ExprNode>().GetText()}";

            return $"case {ctx.enumConstantName.Text}";
        }

        private ExprNode? ForInitExpression([NotNull] ForInitContext ctx)
        {
            if (ctx.expressionList() is not null)
                return this.ExpressionListExpression(ctx.expressionList());

            if (ctx.localVariableDeclaration() is not null)
                return this.StatementExpression(this.Visit(ctx.localVariableDeclaration()));

            return null;
        }

        private ExprNode ExpressionListExpression([NotNull] ExpressionListContext ctx)
        {
            ExprListNode list = this.Visit(ctx).As<ExprListNode>();
            ExprNode[] expressions = list.Expressions.ToArray();
            return expressions.Length == 1 ? expressions[0] : list;
        }

        private DeclStatNode EnhancedForIteratorDeclaration([NotNull] EnhancedForControlContext ctx)
        {
            string modifiers = "";
            int? declSpecsStartLine = null;
            IEnumerable<TagNode> tags = Enumerable.Empty<TagNode>();
            if (ctx.variableModifier() is { } varModifierCtxList && varModifierCtxList.Any()) {
                tags = varModifierCtxList
                    .Where(modCtx => modCtx.annotation() is not null)
                    .Select(modCtx => this.Visit(modCtx.annotation()).As<TagNode>());
                modifiers = string.Join(" ",
                    varModifierCtxList
                        .Select(modCtx => this.ProcessVariableModifier(modCtx))
                        .Where(mod => !string.IsNullOrWhiteSpace(mod)));
                declSpecsStartLine = varModifierCtxList.First().Start.Line;
            }

            TypeNameNode typeName = this.Visit(ctx.typeType()).As<TypeNameNode>();
            declSpecsStartLine ??= typeName.Line;
            var declSpecs = new DeclSpecsNode(declSpecsStartLine ?? ctx.Start.Line, modifiers, typeName);
            DeclNode declarator = this.EnhancedForIteratorDeclarator(ctx.variableDeclaratorId());
            var declList = new DeclListNode(declarator.Line, declarator);

            return tags.Any()
                ? new DeclStatNode(ctx.Start.Line, tags, declSpecs, declList)
                : new DeclStatNode(ctx.Start.Line, declSpecs, declList);
        }

        private DeclNode EnhancedForIteratorDeclarator([NotNull] VariableDeclaratorIdContext ctx)
        {
            var identifier = new IdNode(ctx.Start.Line, ctx.IDENTIFIER().GetText());
            return ctx.LBRACK().Any()
                ? new ArrDeclNode(ctx.Start.Line, identifier)
                : new VarDeclNode(ctx.Start.Line, identifier);
        }

        private ExprNode? StatementExpression(ASTNode? node)
        {
            if (node is null || node is EmptyStatNode)
                return null;
            if (node is ExprNode expression)
                return expression;
            if (node is ExprStatNode exprStat)
                return exprStat.Expression;
            if (node is BlockStatNode block) {
                ExprNode[] expressions = block.Children
                    .Select(this.StatementExpression)
                    .Where(e => e is not null)
                    .Cast<ExprNode>()
                    .ToArray();
                return expressions.Length switch
                {
                    0 => null,
                    1 => expressions[0],
                    _ => new ExprListNode(block.Line, expressions),
                };
            }
            if (node is StatNode stat)
                return this.MarkerExpression(stat.Line, "__linvast_stmt", new IdNode(stat.Line, stat.GetText()));

            return this.MarkerExpression(node.Line, "__linvast_node", new IdNode(node.Line, node.GetText()));
        }

        private ExprStatNode MarkerStatement(int line, string marker, params ExprNode[] args) =>
            new ExprStatNode(line, this.MarkerExpression(line, marker, args));

        private FuncCallExprNode MarkerExpression(int line, string marker, params ExprNode[] args) =>
            args.Any()
                ? new FuncCallExprNode(line, new IdNode(line, marker), new ExprListNode(line, args))
                : new FuncCallExprNode(line, new IdNode(line, marker));

        #endregion
    }
}
