using System.Collections.Generic;
using System.Linq;
using System.Text;
using LINVAST.Nodes;
using Newtonsoft.Json;

namespace LINVAST.Imperative.Nodes
{
    public sealed class TypeDeclNode : DeclNode
    {
        [JsonIgnore]
        public IEnumerable<DeclStatNode> Declarations => this.Children.Skip(3).Cast<DeclStatNode>();

        [JsonIgnore]
        public TypeNameListNode TemplateParameters => this.Children[1].As<TypeNameListNode>();

        [JsonIgnore]
        public TypeNameListNode BaseTypes => this.Children[2].As<TypeNameListNode>();


        public TypeDeclNode(int line, IdNode identifier, TypeNameListNode templateParams, TypeNameListNode baseTypes, IEnumerable<DeclStatNode> declarations)
            : base(line, identifier, new ASTNode[] { templateParams, baseTypes }.Concat(declarations)) { }

        public TypeDeclNode(int line, IdNode identifier, TypeNameListNode templateParams, TypeNameListNode baseTypes, params DeclStatNode[] declarations)
            : base(line, identifier, new ASTNode[] { templateParams, baseTypes }.Concat(declarations)) { }


        public override string GetText()
        {
            var sb = new StringBuilder();
            sb.Append(this.Identifier);
            if (this.TemplateParameters.Types.Any())
                sb.Append('<').Append(this.TemplateParameters.Types).Append('>');
            if (this.BaseTypes.Types.Any())
                sb.Append(" : ").Append(this.BaseTypes.Types);
            sb.AppendLine();
            sb.AppendLine(" { ").AppendJoin("; ", this.Declarations).AppendLine(" }");
            return sb.ToString();
        }
    }

    public sealed class EnumDeclNode : DeclNode
    {
        [JsonIgnore]
        public DeclListNode Constants => this.Children.ElementAt(1).As<DeclListNode>();

        [JsonIgnore]
        public IEnumerable<DeclStatNode> BodyDeclarations => this.Children.Skip(1).Select(c => c.As<DeclStatNode>());


        public EnumDeclNode(int line, IdNode identifier)
            : base(line, identifier) { }

        public EnumDeclNode(int line, IdNode identifier, DeclListNode constants)
            : base(line, identifier, constants) { }
        
        public EnumDeclNode(int line, IdNode identifier, DeclListNode constants, IEnumerable<ASTNode> body)
            : base(line, identifier, new ASTNode[] { constants }.Concat(body)) { }
    }

    public abstract class TypeNode : DeclStatNode
    {
        protected string Category { get; }


        protected TypeNode(int line, string category, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, specifiers, new DeclListNode(line, decl))
        {
            this.Category = category;
        }

        protected TypeNode(int line, string category, IEnumerable<TagNode> tags, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, tags, specifiers, new DeclListNode(line, decl))
        {
            this.Category = category;
        }


        public override string GetText() => $"{string.Join(' ', this.Tags)} {this.Specifiers} {this.Category} {this.DeclaratorList}";
    }

    public sealed class ClassNode : TypeNode
    {
        public ClassNode(int line, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "class", specifiers, decl) { }

        public ClassNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "class", tags, specifiers, decl) { }
    }

    public sealed class StructNode : TypeNode
    {
        public StructNode(int line, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "struct", specifiers, decl) { }

        public StructNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "struct", tags, specifiers, decl) { }
    }

    public sealed class InterfaceNode : TypeNode
    {
        public InterfaceNode(int line, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "interface", specifiers, decl) { }

        public InterfaceNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode specifiers, TypeDeclNode decl)
            : base(line, "interface", tags, specifiers, decl) { }
    }

    public sealed class EnumNode : DeclStatNode
    {
        public EnumNode(int line, DeclSpecsNode specifiers, EnumDeclNode decl)
            : base(line, specifiers, new DeclListNode(line, decl)) { }

        public EnumNode(int line, IEnumerable<TagNode> tags, DeclSpecsNode specifiers, EnumDeclNode decl)
            : base(line, tags, specifiers, new DeclListNode(line, decl)) { }

        public override string ToString() => $"{string.Join(' ', this.Tags)} {this.Specifiers} enum {this.DeclaratorList}";
    }
}
